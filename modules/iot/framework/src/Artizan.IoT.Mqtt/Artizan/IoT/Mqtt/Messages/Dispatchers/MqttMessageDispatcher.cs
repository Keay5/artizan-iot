using Artizan.IoT.Mqtt.Options;
using Artizan.IoT.Mqtt.Topics.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Messages.Dispatchers;

/// <summary>
/// MQTT消息分发器实现（基于Channel和分区的高并发调度）
/// </summary>
public class MqttMessageDispatcher : IMqttMessageDispatcher, ISingletonDependency
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
    /// 分发器配置（分区数、Channel容量等）
    /// </summary>
    private readonly MqttMessageDispatcherOptions _options;

    /// <summary>
    /// 消费者任务集合（每个分区一个消费任务）
    /// </summary>
    private readonly List<Task> _consumerTasks = new();

    private readonly MqttMessagePartitionKeyGenerator _messagePartitionKeyGenerator;

    /// <summary>
    /// 是否已经启动
    /// </summary>
    private bool _isStarted = false;

    /// <summary>
    /// 构造函数（初始化分区通道和依赖组件）
    /// </summary>
    public MqttMessageDispatcher(
        ILogger<MqttMessageDispatcher> logger,
        IMqttMessageRouter messageRouter,
        MqttMessagePartitionKeyGenerator messagePartitionKeyGenerator,
        IOptions<MqttMessageDispatcherOptions> options)
    {
        _logger = logger;
        _messageRouter = messageRouter;
        _messagePartitionKeyGenerator = messagePartitionKeyGenerator;
        _options = options.Value;
        _partitionChannels = new ConcurrentDictionary<string, Channel<MqttMessageContext>>(StringComparer.OrdinalIgnoreCase);
        _partitionLocks =  new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 启动消费者（初始化分区Channel和消费者）
    /// 每个分区一个独立任务，并行处理
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            _logger.LogWarning("[MQTT消息分发器]已启动，无需重复操作");
            return;
        }

        // 预创建分区通道和消费者（避免动态创建开销）
        for (int i = 0; i < _options.PartitionCount; i++)
        {
            var partitionKey = i.ToString();
            _partitionChannels.GetOrAdd(
                partitionKey,
                _ => Channel.CreateBounded<MqttMessageContext>(new BoundedChannelOptions(_options.ChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait, // 队列满时等待（避免丢消息）
                    SingleReader = true,
                    SingleWriter = false
                })
            );

            // 启动分区消费者
            _consumerTasks.Add(ConsumePartitionAsync(partitionKey, cancellationToken));
        }

        _isStarted = true;
        _logger.LogInformation("[MQTT消息分发器] 启动完成 | 分区数：{PartitionCount} | Channel容量：{ChannelCapacity} | BatchSize：{BatchSize} | BatchTimeout：{BatchTimeout}(毫秒) | PartitionStrategy：{PartitionStrategy}",
            _options.PartitionCount, _options.ChannelCapacity, _options.BatchSize, _options.BatchTimeout.TotalMilliseconds, _options.PartitionStrategy);
    }

    /// <summary>
    /// 停止分发器（完成剩余消息处理后退出）
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            _logger.LogWarning("[MQTT消息分发器] 未启动，无需停止");
            return;
        }

        // 标记所有Channel为完成（停止接收新消息）
        foreach (var channel in _partitionChannels.Values)
        {
            channel.Writer.Complete();
        }

        // 等待所有消费者任务结束
        await Task.WhenAll(_consumerTasks);
        _consumerTasks.Clear();

        _isStarted = false;
        _logger.LogInformation("[MQTT消息分发器] 已停止，剩余消息处理完成");
    }

    /// <summary>
    /// 入队消息（高并发入口，非阻塞）:提交消息到分区Channel（生产者逻辑）
    /// </summary>
    public async Task EnqueueAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            throw new InvalidOperationException("[MQTT消息分发器] 未启动，无法入队消息");
        }

        try
        {
            // 按设备维度分区（确保单设备消息有序）
            // 按ProductKey+DeviceName哈希计算分区键（确保单设备消息入同一分区）
            var partitionKey = _messagePartitionKeyGenerator.GetPartitionKey(context.ProductKey, context.DeviceName);
            var channel = _partitionChannels.GetOrAdd(
                partitionKey,
                _ => Channel.CreateBounded<MqttMessageContext>(_options.ChannelCapacity)
            );

            // 非阻塞入队（若Channel满则等待，避免OOM）
            await channel.Writer.WriteAsync(context, cancellationToken);
            _logger.LogDebug("[{TraceId}][MQTT消息分发器] 消息已入队 | 分区：{PartitionKey}",
                context.TraceId, partitionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}][MQTT消息分发器] 消息入队失败 | ProductKey={ProductKey} | DeviceName={DeviceName}",
                context.TraceId, context.ProductKey, context.DeviceName);
            throw;
        }
    }

    /// <summary>
    /// 分区消费者逻辑（批量处理消息）
    /// </summary>
    private async Task ConsumePartitionAsync(string partitionKey, CancellationToken cancellationToken)
    {
        var channel = _partitionChannels[partitionKey];
        var partitionLock = _partitionLocks.GetOrAdd(partitionKey, _ => new SemaphoreSlim(1, 1));
        var batchList = new List<MqttMessageContext>(_options.BatchSize);
        var batchTimer = new PeriodicTimer(_options.BatchTimeout);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 批量读取消息（达到阈值或超时）
                while (batchList.Count < _options.BatchSize && channel.Reader.TryRead(out var context))
                {
                    batchList.Add(context);
                }

                // 触发批量处理
                if (batchList.Count > 0 || await batchTimer.WaitForNextTickAsync(cancellationToken))
                {
                    if (batchList.Count == 0)
                    {
                        continue;
                    }

                    await partitionLock.WaitAsync(cancellationToken);
                    try
                    {
                        // 批量路由消息到Handler
                        await ProcessBatchAsync(batchList, cancellationToken);
                    }
                    finally
                    {
                        partitionLock.Release();
                        batchList.Clear();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"[MQTT消息分发器] 分区[{partitionKey}]消费者已取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MQTT消息分发器] 分区[{partitionKey}]消费者异常");
        }
        finally
        {
            batchTimer.Dispose();
            partitionLock.Dispose();
        }
    }

    /// <summary>
    /// 批量处理消息（调用路由系统）
    /// </summary>
    private async Task ProcessBatchAsync(List<MqttMessageContext> batch, CancellationToken cancellationToken)
    {
        var batchStopwatch = Stopwatch.StartNew();
        try
        {
            foreach (var context in batch)
            {
                try
                {
                    // 调用路由系统
                    await _messageRouter.RouteMessageAsync(context, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{context.TraceId}][MQTT消息分发器] 批量处理消息失败");
                    context.SetGlobalException(ex);
                }
                finally
                {
                    // 释放上下文资源
                    context.Dispose();
                }
            }

            _logger.LogInformation($"[MQTT消息分发器] 批量处理「{batch.Count}」条消息完成，耗时：{batchStopwatch.Elapsed.TotalMilliseconds}ms");
        }
        finally
        {
            batchStopwatch.Stop();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        foreach (var channel in _partitionChannels.Values)
        {
            channel.Writer.Complete();
        }
        foreach (var lck in _partitionLocks.Values)
        {
            lck.Dispose();
        }
        _logger.LogInformation("[MQTT消息分发器] 资源已释放");
    }
}
