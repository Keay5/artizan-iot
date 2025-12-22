using System.Collections.Generic;
using System.Collections.Immutable;

namespace Artizan.IoT.Mqtts.Topics.Registrys;

/// <summary>
/// MQTT动态Topic注册中心接口
/// 
/// 【设计理念】：
/// 0.依赖倒置原则 - 上层依赖接口，下层实现，便于替换和测试
/// 1. 抽象核心能力：注册/移除/路由/查询，便于替换实现（如分布式注册中心）；
/// 2. 适配MqttMessageContext：所有方法围绕上下文设计，无缝整合；
/// 3. 线程安全：接口契约隐含线程安全要求，实现类必须保障高并发安全。
/// </summary>
public interface IDynamicMqttTopicRegistry
{
    /// <summary>
    /// 注册Topic路由规则
    /// </summary>
    void Register(DynamicMqttTopicMetadata metadata);

    /// <summary>
    /// 移除指定Topic模板的路由规则
    /// </summary>
    bool Unregister(string topicTemplate);

    /// <summary>
    /// 批量移除指定前缀的Topic路由
    /// </summary>
    int UnregisterByPrefix(string prefix);

    /// <summary>
    /// 获取当前已注册的所有Topic模板（监控/调试用）
    /// </summary>
    IReadOnlyCollection<string> GetRegisteredTemplates();

    /// <summary>
    /// 获取按优先级排序后的路由元数据列表
    /// </summary>
    /// <returns></returns>
    ImmutableList<DynamicMqttTopicMetadata> GetSortedTopics();
}
