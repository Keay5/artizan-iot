using Artizan.IoT.Concurrents;
using Artizan.IoT.Mqtts.Etos;
using Artizan.IoTHub.Products.MessageParsings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Artizan.IoTHub.Mqtts.Messages;

/// <summary>
/// MQTT事件处理器: 解析主题消息
/// </summary>
public class ParsingMqttTopicMessageEventHandler : 
    ConcurrentPartitionedMessageDispatcher<MqttClientPublishTopicEto>,
    IDistributedEventHandler<MqttClientPublishTopicEto>,
    ISingletonDependency
{
    private readonly TopicMessageParsingManager _topicMessageParsingManager; // 注入业务逻辑类

    /// <summary>
    /// 构造函数（仅初始化调度相关资源）
    /// </summary>
    public ParsingMqttTopicMessageEventHandler(
        ILogger<ParsingMqttTopicMessageEventHandler> logger,
        TopicMessageParsingManager topicMessageParsingManager)
        : base(logger, 10000)
    {
        _topicMessageParsingManager = topicMessageParsingManager;
    }

    /// <summary>
    /// 获取分区Key：ProductKey+DeviceName，确保同一设备的消息串行处理
    /// 设计思路：避免设备消息乱序（如设备上报的时序数据）
    /// </summary>
    /// <param name="eventData">MQTT发布事件数据</param>
    /// <returns>设备唯一标识（ProductKey_DeviceName）</returns>
    protected override string GetPartitionKey(MqttClientPublishTopicEto eventData)
    {
        return $"{eventData.ProductKey}_{eventData.DeviceName}";
    }

    public async Task HandleEventAsync(MqttClientPublishTopicEto eventData)
    {
        await EnqueueMessageAsync(eventData);
    }

    /// <summary>
    /// 处理MQTT消息的业务逻辑
    /// </summary>
    /// <param name="eventData">MQTT发布事件数据</param>
    /// <param name="consumerId">消费者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    protected override async Task ProcessMessageAsync(
        MqttClientPublishTopicEto eventData,
        int consumerId,
        CancellationToken cancellationToken)
    {
        // 获取分区Key：由子类实现，确保同一实体的消息串行处理
        var partitionKey = GetPartitionKey(eventData);
        int dynamicWaitMs = GetDynamicLockWaitMs(partitionKey);
        // 增加超时控制，避免业务逻辑阻塞分区:
        // 消息处理（如数据库操作、网络调用）若阻塞，会占用分区消费线程，导致同一分区消息堆积，甚至引发连锁反应（如线程池耗尽）。
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(dynamicWaitMs); 

        await _topicMessageParsingManager.TryParseTopicMessageAsync(eventData, consumerId, timeoutCts.Token).ConfigureAwait(false);
    }

    #region 日志定制（重写基类日志方法，添加MQTT特定上下文）
    /// <inheritdoc />
    protected override void LogMessageEnqueued(MqttClientPublishTopicEto eventData)
    {
        Logger.LogDebug("[消息入队] [成功] | [{TrackId}] 消息已写入处理通道 | 设备：{ProductKey}/{DeviceName} | 主题：{Topic}",
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            eventData.MqttTopic);
    }

    /// <inheritdoc />
    protected override void LogMessageEnqueueCanceled(MqttClientPublishTopicEto eventData)
    {
        Logger.LogWarning("[消息入队] [被取消] | MQTT消息处理器[{Name}] | 消息入队被取消（服务关闭）【无法处理】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            nameof(ParsingMqttTopicMessageEventHandler),
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogMessageEnqueueFailed(MqttClientPublishTopicEto eventData, Exception ex)
    {
        Logger.LogError(ex, "[消息入队] [失败] | MQTT消息处理器[{Name}] | 消息入队失败【无法处理】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            nameof(ParsingMqttTopicMessageEventHandler),
            eventData?.MqttTrackId ?? "未知",
            eventData?.ProductKey ?? "未知",
            eventData?.DeviceName ?? "未知",
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogProcessingAbortedDueToDisposal(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogError("[消息通道][Consumer:{ConsumerId}] | 服务已释放【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogProcessingCancelled(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogWarning("[消息通道][Consumer:{ConsumerId}] | 服务关闭/取消【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogLockWaitTimeout(MqttClientPublishTopicEto eventData, int consumerId, int dynamicWaitMs)
    {
        Logger.LogError("[消息通道][Consumer:{ConsumerId}] | 锁等待超时（{dynamicWaitMs} 秒）【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            dynamicWaitMs / 1000,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogProcessingStarted(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [开始] | [{TrackId}] 开始执行业务逻辑 | 设备：{ProductKey}/{DeviceName}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName);
    }

    /// <inheritdoc />
    protected override void LogProcessingCompleted(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [完成] | [{TrackId}] 业务逻辑执行完成 | 设备：{ProductKey}/{DeviceName}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName);
    }

    /// <inheritdoc />
    protected override void LogProcessingCanceledException(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogWarning("[消息消费][Consumer:{ConsumerId}] [被取消] | 处理被取消【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogProcessingException(MqttClientPublishTopicEto eventData, int consumerId, Exception ex)
    {
        Logger.LogError(ex, "[消息消费][Consumer:{ConsumerId}] [未处理] | 处理失败【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 主题：{Topic} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            eventData.MqttTopic,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogMessageProcessingFailed(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogCritical("[消息消费][Consumer:{ConsumerId}] [未处理] | 消息最终判定为无法处理 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 主题：{Topic} | 消息内容摘要：{PayloadSummary}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            eventData.MqttTopic,
            GetPayloadSummary(eventData.MqttPayload));
    }

    /// <inheritdoc />
    protected override void LogLockReleased(MqttClientPublishTopicEto eventData, int consumerId)
    {
        Logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [锁] | [{TrackId}] 锁释放成功 | 设备：{ProductKey}/{DeviceName}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName);
    }

    /// <inheritdoc />
    protected override void LogLockAlreadyDisposed(MqttClientPublishTopicEto eventData, int consumerId, ObjectDisposedException ex)
    {
        Logger.LogError(ex, "[消息消费][Consumer:{ConsumerId}] [锁] [异常] | 锁已被释放（重复释放）【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
            consumerId,
            eventData.MqttTrackId,
            eventData.ProductKey,
            eventData.DeviceName,
            Interlocked.Read(ref _unprocessedMessageCount));
    }

    /// <inheritdoc />
    protected override void LogLockCleanedUp(MqttClientPublishTopicEto eventData, int consumerId, string partitionKey)
    {
        Logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [锁] | [{TrackId}] 清理设备[{PartitionKey}]的闲置锁（引用计数为0） | 设备：{ProductKey}/{DeviceName}",
            consumerId, eventData.MqttTrackId, partitionKey, eventData.ProductKey, eventData.DeviceName);
    }

    /// <inheritdoc />
    protected override void LogLockCleanupException(MqttClientPublishTopicEto eventData, int consumerId, string partitionKey, Exception ex)
    {
        Logger.LogDebug(ex, "[消息消费][Consumer:{ConsumerId}] [锁] | [{TrackId}] 清理设备[{PartitionKey}]锁时异常 | 设备：{ProductKey}/{DeviceName}",
            consumerId, eventData.MqttTrackId, partitionKey, eventData.ProductKey, eventData.DeviceName);
    }

    #endregion

    #region 辅助方法
    /// <summary>
    /// 获取Payload摘要（避免日志过大，同时保留关键信息）
    /// </summary>
    /// <param name="payload">原始Payload</param>
    /// <returns>Payload摘要</returns>
    protected string GetPayloadSummary(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
        {
            return "空Payload";
        }

        try
        {
            // 转换为字符串，最多显示前100个字符
            var payloadStr = System.Text.Encoding.UTF8.GetString(payload);
            return payloadStr.Length > 100 ? $"{payloadStr[..100]}..." : payloadStr;
        }
        catch
        {
            // 非UTF8编码，显示字节长度
            return $"非UTF8编码，字节长度：{payload.Length}";
        }
    }
    #endregion
}