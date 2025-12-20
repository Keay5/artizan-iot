using Artizan.IoT.Messages.PostProcessors;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Dispatchers;

/// <summary>
/// MQTT消息分发器实现（基于Channel和分区的高并发调度）
/// </summary>
public class MqttMessageDispatcher : IMqttMessageDispatcher
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<MqttMessageDispatcher> _logger;

    /// <summary>
    /// 分区通道容器（按设备哈希分区，每个分区一个Channel）
    /// 设计：ConcurrentDictionary确保线程安全，分区数可配置（如32/64）。
    /// </summary>
    private readonly ConcurrentDictionary<string, Channel<MqttMessageContext>> _partitionChannels;

    /// <summary>
    /// 分区锁（确保单分区内消息有序处理）
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _partitionLocks;

    /// <summary>
    /// 消息路由（负责将消息分发到对应Handler）
    /// </summary>
    private readonly IMqttMessageRouter _messageRouter;

    /// <summary>
    /// 后处理插件工厂（获取并执行扩展插件）
    /// </summary>
    private readonly IMessagePostProcessorFactory _processorFactory;

    /// <summary>
    /// 分发器配置（分区数、Channel容量等）
    /// </summary>
    private readonly MqttDispatcherOptions _options;

    /// <summary>
    /// 消费者任务集合（每个分区一个消费任务）
    /// </summary>
    private readonly List<Task> _consumerTasks = new();

    /// <summary>
    /// 构造函数（初始化分区通道和依赖组件）
    /// </summary>
    public MqttMessageDispatcher(
        ILogger<MqttMessageDispatcher> logger,
        IMqttMessageRouter messageRouter,
        IMessagePostProcessorFactory processorFactory,
        IOptions<MqttDispatcherOptions> options)
    {
        _logger = logger;
        _messageRouter = messageRouter;
        _processorFactory = processorFactory;
        _options = options.Value;
        _partitionChannels = 
            new ConcurrentDictionary<string, Channel<MqttMessageContext>>(StringComparer.OrdinalIgnoreCase);
        _partitionLocks = 
            new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 提交消息到分区Channel（生产者逻辑）
    /// </summary>
    public async Task EnqueueAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
    {
        // 按ProductKey+DeviceName哈希计算分区键（确保单设备消息入同一分区）
        var partitionKey = GetPartitionKey(context.ProductKey, context.DeviceName);
        var channel = _partitionChannels.GetOrAdd(
            partitionKey, 
            _ => Channel.CreateBounded<MqttMessageContext>(_options.ChannelCapacity)
        );

        // 非阻塞入队（若Channel满则等待，避免OOM）
        await channel.Writer.WriteAsync(context, cancellationToken);
    }

    /// <summary>
    /// 启动消费者（每个分区一个独立任务，并行处理）
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 预创建分区通道和消费者（避免动态创建开销）
        for (int i = 0; i < _options.PartitionCount; i++)
        {
            var partitionKey = i.ToString();
            _partitionChannels.GetOrAdd(partitionKey, _ =>
                Channel.CreateBounded<MqttMessageContext>(_options.ChannelCapacity));
            _consumerTasks.Add(StartConsumerAsync(partitionKey, cancellationToken));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止分发器（完成剩余消息处理后退出）
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        // 标记所有Channel为完成状态（不再接收新消息）
        foreach (var channel in _partitionChannels.Values)
            channel.Writer.Complete();

        // 等待所有消费者任务结束
        await Task.WhenAll(_consumerTasks);
        _consumerTasks.Clear();
    }

    /// <summary>
    /// 分区消费者逻辑（消费Channel消息并处理）
    /// </summary>
    private async Task StartConsumerAsync(string partitionKey, CancellationToken cancellationToken)
    {
        var channel = _partitionChannels[partitionKey];
        var partitionLock = _partitionLocks.GetOrAdd(partitionKey, _ => new SemaphoreSlim(1, 1));

        await foreach (var context in channel.Reader.ReadAllAsync(cancellationToken))
        {
            // 单分区内加锁，确保消息有序处理
            await partitionLock.WaitAsync(cancellationToken);
            try
            {
                // 1. 路由到对应Handler处理
                await _messageRouter.RouteMessageAsync(context, cancellationToken);

                // 2. 执行后处理插件（如缓存、入库）
                if (context.IsParsedSuccess)
                    await ExecutePostProcessorsAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                context.SetGlobalException(ex);
                // 记录异常日志（关联TraceId）
                _logger.LogError(ex, $"[{context.TraceId}] 消息处理失败");
            }
            finally
            {
                partitionLock.Release();
                context.Dispose(); // 释放上下文资源
            }
        }
    }

    /// <summary>
    /// 执行后处理插件（按优先级排序）
    /// </summary>
    private async Task ExecutePostProcessorsAsync(MqttMessageContext context, CancellationToken cancellationToken)
    {
        var processors = _processorFactory.GetProcessors<MqttMessageContext>()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority);

        foreach (var processor in processors)
        {
            await processor.ProcessAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// 计算分区键（哈希取模，确保均匀分布）
    /// </summary>
    private string GetPartitionKey(string productKey, string deviceName)
    {
        var key = $"{productKey}:{deviceName}";
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(key);
        return Math.Abs(hash % _options.PartitionCount).ToString();
    }
}

