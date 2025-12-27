using Artizan.IoT.Alinks.DataObjects.MessageCommunications;
using Artizan.IoT.Messages.PostProcessors;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Things.Caches;
using Artizan.IoT.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Mqtts.Messages.PostProcessors.Caches;

public class MqttCacheMessagePostProcessor :
    IMessagePostProcessor<MqttMessageContext>,
    ISingletonDependency
{
    private readonly ILogger<MqttCacheMessagePostProcessor> _logger;

    #region IMessagePostProcessor 接口实现
    /// <summary>
    /// 处理器优先级（值越大，执行顺序越靠后）
    /// 【设计考量】
    /// - 优先级50：确保缓存处理器在消息解析、验证等前置处理器之后执行
    /// - 避免处理未解析/验证失败的数据，保证缓存数据的有效性
    /// </summary>
    public int Priority => MqttMessagePostProcessorPriorityConstants.MqttCacheMessagePostProcessorPriority;

    /// <summary>
    /// 处理器启用状态（从ABP配置动态读取）
    /// 【设计考量】
    /// - 支持动态启用/禁用：无需重启服务，便于运维调试（如排查缓存问题时临时禁用）
    /// - 同步读取：Result不会阻塞（优化：可以设计为从ABP SettingProvider获取动态值缓存）
    /// </summary>
    public bool IsEnabled => true;

    #endregion

    private readonly ConcurrentQueue<ThingPropertyDataCacheItem> _batchQueue = new();
    private readonly SemaphoreSlim _batchLock = new(1, 1);
    private int _batchSize = 100;  // 批量写入阈值
    private int _batchTimeoutSeconds = 1; //批量写入超时时间（秒）

    private readonly IThingPropertyDataCacheManager _propertyDataCacheManager;
    // 线程安全记录第一条数据入队时间（解决volatile编译错误）
    private readonly object _firstEnqueueTimeLock = new object();
    private DateTime? _firstEnqueueUtcTime; // 队列第一条数据的UTC入队时间

    private readonly IThingPropertyHistoryDataCacheManager _propertyHistoryDataCacheManager;

    public MqttCacheMessagePostProcessor(
        ILogger<MqttCacheMessagePostProcessor> logger,
        IThingPropertyDataCacheManager propertyDataCacheManager,
        IThingPropertyHistoryDataCacheManager propertyHistoryDataCacheManager)
    {
        _logger = logger;
        _propertyDataCacheManager = propertyDataCacheManager;
        _propertyHistoryDataCacheManager = propertyHistoryDataCacheManager;
    }

    #region IMessagePostProcessor 接口实现
    public async Task ProcessAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
    {
        await WriteThingPropertyDataToCacheAsync(context, cancellationToken);
    }

    private async Task WriteThingPropertyDataToCacheAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
    {
        // 构建缓存项：标准化格式
        var thingPropertyDataCacheItems = ExtractThingPropertyDataCacheItem(context);
        foreach (var cacheItem in thingPropertyDataCacheItems)
        {
            bool isQueueEmptyBeforeEnqueue = _batchQueue.Count == 0;
            _batchQueue.Enqueue(cacheItem);

            // 记录队列第一条数据的入队时间（线程安全）
            if (isQueueEmptyBeforeEnqueue)
            {
                lock (_firstEnqueueTimeLock)
                {
                    // 二次检查：避免多线程同时入队导致时间被覆盖
                    if (_batchQueue.Count == 1)
                    {
                        _firstEnqueueUtcTime = DateTime.UtcNow;
                    }
                }
            }
        }

        // 触发批量条件：达到阈值 || 超时
        if (IsBatchTriggerConditionMet())
        {
            await _batchLock.WaitAsync(cancellationToken);
            try
            {
                // 双重检查：避免多线程竞争导致重复处理
                if (!IsBatchTriggerConditionMet()) return;

                var batch = new List<ThingPropertyDataCacheItem>();
                // 从队列提取数据：最多提取_batchSize条，避免单次处理过多导致超时
                while (batch.Count < _batchSize && _batchQueue.TryDequeue(out var item))
                {
                    batch.Add(item);
                }

                if (batch.Count > 0)
                {
                    _logger.LogInformation($"[{context.TraceId}][Thing Property 最新数据] 触发批量写入物模型属性缓存，条数：{batch.Count}");
                    
                    // 批量写入缓存
                    await _propertyDataCacheManager.SetManyAsync(batch);
                }

                // 队列清空后重置入队时间（避免下次误判超时）
                lock (_firstEnqueueTimeLock)
                {
                    if (_batchQueue.Count == 0)
                    {
                        _firstEnqueueUtcTime = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{context.TraceId}] 批量写入物模型属性缓存失败");
                throw; // 按需选择是否抛出，或仅记录日志
            }
            finally
            {
                _batchLock.Release();
            }
        }
    }

    /// <summary>
    /// 判断批量触发条件（达到阈值 || 超时）
    /// 「达到阈值立即触发」、「超时立即触发」
    /// </summary>
    private bool IsBatchTriggerConditionMet()
    {
        // 无数据直接返回false
        if (_batchQueue.Count == 0)
        {
            return false; 
        }

        // 条件1：达到批量阈值
        bool reachBatchSize = _batchQueue.Count >= _batchSize;

        // 条件2：超时（第一条数据入队时间 + 超时秒数 ≤ 当前UTC时间）
        bool isTimeout = false;
        lock (_firstEnqueueTimeLock)
        {
            isTimeout = _firstEnqueueUtcTime.HasValue
                     && DateTime.UtcNow - _firstEnqueueUtcTime.Value >= TimeSpan.FromSeconds(_batchTimeoutSeconds);
        }

        // 调试日志：便于排查触发逻辑问题
        if (reachBatchSize)
        {
            _logger.LogDebug($"批量触发条件满足：队列条数({_batchQueue.Count}) ≥ 阈值({_batchSize})");
        }
        else if (isTimeout)
        {
            _logger.LogDebug($"批量触发条件满足：第一条数据入队时间({_firstEnqueueUtcTime}) 超时({_batchTimeoutSeconds}秒)");
        }

        return reachBatchSize || isTimeout;
    }

    private List<ThingPropertyDataCacheItem> ExtractThingPropertyDataCacheItem(MqttMessageContext context)
    {
        List<ThingPropertyDataCacheItem> cacheItems = new List<ThingPropertyDataCacheItem>();

        try
        {
            // 数据校验：上下文无效（未解析成功/无数据），跳过缓存写入
            if (!context.IsParsedSuccess || context.ParsedData == null)
            {
                _logger.LogWarning($"[{context.TraceId}] MQTT消息上下文无效（未解析成功/无数据），跳过缓存写入");
                return [];
            }

            var parseData = (PropertyPostRequest)context.ParsedData;
            if (parseData != null)
            {
                foreach (var param in parseData.Params)
                {
                    var cacheItem = new ThingPropertyDataCacheItem();
                    cacheItem.ProductKey = context.ProductKey;
                    cacheItem.DeviceName = context.DeviceName;
                    cacheItem.PropertyIdentifier = param.Key;
                    cacheItem.Value = param.Value.Value;
                    cacheItem.DataType = param.Value.GetType().FullName;
                    cacheItem.TimeStamp = TimeStampUtil.TryConvertMillisecondsTimestampToUtcDateTime(param.Value.Time) ?? DateTime.UtcNow;

                    cacheItems.Add(cacheItem);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{context.TraceId}] 提取物模型属性缓存项失败");
            return [];
        }

        return cacheItems;
    }

    #endregion
}