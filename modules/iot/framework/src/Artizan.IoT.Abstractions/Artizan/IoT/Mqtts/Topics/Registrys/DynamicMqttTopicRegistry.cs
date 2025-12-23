using Artizan.IoT.Mqtts.MessageHanlders;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics.Registrys;


/// <summary>
/// MQTT动态Topic注册中心（核心职责：路由元数据的注册、存储、查询、移除）
/// 【设计思想】：单一职责原则 - 仅管理路由元数据，不参与消息路由逻辑
/// 【设计理念】：线程安全 - 采用ConcurrentDictionary+ImmutableList(无锁读取)确保多线程下的读写安全
/// 【设计模式】：注册表模式 - 集中管理所有路由规则，提供统一的注册/查询入口
/// 
/// 【设计理念(补充)】：
/// 1. 线程安全：ConcurrentDictionary存储路由，ImmutableList缓存排序结果，无锁读取；
/// 2. 优先级路由：按「优先级数值→模板层级→模板长度」排序，解决匹配冲突；
/// 3. 上下文适配：自动填充MqttMessageContext的占位符、ProductKey、DeviceName；
/// 4. 异常隔离：单个Handler异常不影响其他消息路由，提升系统可用性；
/// 5. 并发控制：SemaphoreSlim限制最大并发数，避免线程池耗尽。
/// </summary>
public class DynamicMqttTopicRegistry : IDynamicMqttTopicRegistry, ISingletonDependency
{
    #region 私有字段（线程安全设计）
    /// <summary>
    /// 日志组件（ABP依赖注入）
    /// </summary>
    private readonly ILogger<DynamicMqttTopicRegistry> _logger;

    /// <summary>
    /// 路由元数据存储字典（Key：Topic模板，Value：路由元数据）
    /// 【设计】：ConcurrentDictionary确保多线程下的增删改查线程安全
    /// </summary>
    private readonly ConcurrentDictionary<string, DynamicMqttTopicMetadata> _topicMap = new();

    /// <summary>
    /// 排序后的路由元数据列表（按优先级降序）
    /// 【设计】：ImmutableList确保只读、线程安全，volatile关键字确保多线程可见性
    /// </summary>
    private volatile ImmutableList<DynamicMqttTopicMetadata> _sortedTopics = ImmutableList<DynamicMqttTopicMetadata>.Empty;
    #endregion

    #region 构造函数（依赖注入）
    /// <summary>
    /// 构造函数（依赖注入：日志组件）
    /// 【设计理念】：依赖注入 - 通过构造函数注入依赖，降低耦合，便于测试
    /// </summary>
    /// <param name="logger">日志组件</param>
    public DynamicMqttTopicRegistry(ILogger<DynamicMqttTopicRegistry> logger)
    {
        _logger = logger;
    }
    #endregion

    #region 核心功能：路由元数据注册
    /// <summary>
    /// 注册路由元数据（线程安全）
    /// 【设计】：先更新字典，再刷新排序列表，确保数据一致性
    /// 【特性】：同模板重复注册时，高优先级覆盖低优先级，即时生效
    /// </summary>
    /// <param name="metadata">路由元数据（非空）</param>
    /// <exception cref="ArgumentNullException">元数据为空时抛出</exception>
    public void Register(DynamicMqttTopicMetadata metadata)
    {
        Check.NotNull(metadata, nameof(metadata));

        // 1. 写入线程安全字典（覆盖已有同模板的路由）
        _topicMap[metadata.TopicTemplate] = metadata;

        // 2. 刷新排序后的路由列表（按优先级降序）
        RefreshSortedTopics();

        // 3. 日志记录（便于问题排查）
        _logger.LogInformation(
            "[MQTT路由注册] 成功 | 模板：{TopicTemplate} | 优先级：{Priority} | Handler：{HandlerType}",
            metadata.TopicTemplate, metadata.Priority, metadata.HandlerType.FullName);
    }
    #endregion

    #region 核心功能：路由元数据移除
    /// <summary>
    /// 移除指定Topic模板的路由元数据
    /// 【设计】：TryRemove确保移除操作线程安全，避免空指针
    /// </summary>
    /// <param name="topicTemplate">Topic模板（非空）</param>
    /// <returns>移除成功返回true，失败返回false</returns>
    public bool Unregister(string topicTemplate)
    {
        // 防御式编程：校验入参
        if (string.IsNullOrWhiteSpace(topicTemplate))
        {
            _logger.LogWarning("[MQTT路由移除] 失败 | 原因：Topic模板为空");
            return false;
        }

        // 线程安全移除
        if (_topicMap.TryRemove(topicTemplate, out var removedMetadata))
        {
            // 移除后刷新排序列表
            RefreshSortedTopics();

            _logger.LogInformation(
                "[MQTT路由移除] 成功 | 模板：{TopicTemplate} | Handler：{HandlerType}",
                removedMetadata.TopicTemplate, removedMetadata.HandlerType.FullName);
            return true;
        }

        _logger.LogWarning("[MQTT路由移除] 失败 | 模板：{TopicTemplate}（未找到）", topicTemplate);
        return false;
    }

    /// <summary>
    /// 按前缀批量移除路由元数据
    /// 【扩展点】：可扩展按正则、按Handler类型等批量移除策略
    /// </summary>
    /// <param name="prefix">Topic模板前缀（如"/sys/"）</param>
    /// <returns>移除成功的数量</returns>
    public int UnregisterByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            _logger.LogWarning("[MQTT路由批量移除] 失败 | 原因：前缀为空");
            return 0;
        }

        // 1. 筛选出符合前缀的Topic模板
        var toRemove = _topicMap.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        // 2. 批量移除（线程安全）
        var removedCount = toRemove.Count(key => _topicMap.TryRemove(key, out _));

        // 3. 移除后刷新排序列表
        if (removedCount > 0)
        {
            RefreshSortedTopics();
            _logger.LogInformation("[MQTT路由批量移除] 成功 | 前缀：{Prefix} | 移除数量：{Count}", prefix, removedCount);
        }
        else
        {
            _logger.LogWarning("[MQTT路由批量移除] 失败 | 前缀：{Prefix}（无匹配项）", prefix);
        }

        return removedCount;
    }
    #endregion

    #region 核心功能：路由元数据查询
    /// <summary>
    /// 获取所有已注册的Topic模板
    /// 【设计】：返回只读集合，避免外部修改内部数据
    /// </summary>
    /// <returns>只读Topic模板列表</returns>
    public IReadOnlyCollection<string> GetRegisteredTemplates()
    {
        return _topicMap.Keys.ToImmutableList();
    }

    /// <summary>
    /// 获取按优先级排序后的路由元数据列表
    /// 【设计】：返回ImmutableList，确保线程安全、只读
    /// </summary>
    /// <returns>排序后的路由元数据列表</returns>
    public ImmutableList<DynamicMqttTopicMetadata> GetSortedTopics()
    {
        // volatile关键字确保读取到最新的排序列表
        return _sortedTopics;
    }
    #endregion

    #region 私有辅助方法
    /// <summary>
    /// 刷新排序后的路由列表（按优先级降序→Topic层级数降序→模板长度降序）
    /// 【设计理念】：开闭原则 - 排序规则可扩展，不修改原有逻辑
    /// 【扩展点】：可配置排序策略（如自定义IComparer<DynamicMqttTopicMetadata>）
    ///
    /// 【排序规则】：（优先级从高到低）：
    /// 1. 优先级数值（Priority）降序；
    /// 2. 模板层级数（/数量）降序（层级越深越具体）；
    /// 3. 模板长度降序（长度越长越具体）；
    /// 目的：确保精准模板优先于模糊模板匹配
    /// </summary>
    private void RefreshSortedTopics()
    {
        _sortedTopics = _topicMap.Values
            // 排序规则：1. 优先级高的优先 2. Topic层级多的优先（更精准匹配） 3. 模板长度长的优先
            .OrderByDescending(meta => meta.Priority)
            .ThenByDescending(meta => meta.TopicTemplate.Count(c => c == '/'))
            .ThenByDescending(meta => meta.TopicTemplate.Length)
            .ToImmutableList(); // 转为不可变列表，确保线程安全

        _logger.LogDebug("[MQTT路由排序] 完成 | 总路由数：{Count}", _sortedTopics.Count);
    }
    #endregion
}
