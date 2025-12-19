using Artizan.IoT.Mqtts.Messages.Parsers;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Messages;

/// <summary>
/// MQTT消息分发器
/// </summary>
public class MqttMessageDispatcher : ISingletonDependency
{
    private readonly ILogger<MqttMessageDispatcher> _logger;

    // 1. 背压控制：Channel作为消息缓冲队列（生产-消费模型）
    private readonly Channel<MqttMessageContext> _messageChannel;
    // 2. 依赖注入：解析器、工作流步骤、策略注册表等
    private readonly IEnumerable<IMqttMessageParser> _parsers;
    private readonly IEnumerable<IMqttWorkflowStep> _workflowSteps;
    //private readonly IMetrics _metrics; // 监控指标（如Prometheus）
    private Task _consumeTask;
    private CancellationTokenSource _cts;

    // 初始化Channel（配置容量，防止内存溢出）
    public MqttMessageDispatcher(
        ILogger<MqttMessageDispatcher> logger,
        IEnumerable<IMqttMessageParser> parsers,
        IEnumerable<IMqttWorkflowStep> workflowSteps)
        //IMetrics metrics
    {
        _parsers = parsers;
        _workflowSteps = workflowSteps.Where(s => s.IsEnabled); // 过滤禁用的步骤
        _logger = logger;
        //_metrics = metrics;

        // 配置Channel：有界队列（容量10000），超出则生产端等待（背压）
        var channelOptions = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait, // 队列满时阻塞生产端
            SingleReader = false,
            SingleWriter = false
        };
        _messageChannel = Channel.CreateBounded<MqttMessageContext>(channelOptions);
    }

    #region 初始化配置 Channel 和容错策略
    // 启动消费任务
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumeTask = ConsumeMessagesAsync(_cts.Token);
        _logger.LogInformation("MqttPublishingService started, channel capacity: {Capacity}", 10000);
        return Task.CompletedTask;
    }

    // 停止消费
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        await _consumeTask;
        _messageChannel.Writer.Complete();
        _logger.LogInformation("MqttPublishingService stopped");
    }

    #endregion

    #region 生产端

    // 生产端：接收MQTT消息并写入Channel
    public async Task EnqueueMessageAsync(MqttMessageContext context)
    {
        try
        {
            // 写入Channel（队列满时阻塞，避免消息丢失）
            await _messageChannel.Writer.WriteAsync(context, _cts.Token);
            //_metrics.IncrementCounter("mqtt.message.received"); // 记录接收数
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Enqueue message canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue MQTT message");
            //_metrics.IncrementCounter("mqtt.message.enqueue.failed");
        }
    }

    #endregion

    #region 消费者端

    // 消费端：处理Channel中的消息（核心流程）
    private async Task ConsumeMessagesAsync(CancellationToken token)
    {
        // 工作池配置：不同步骤用不同的工作池（隔离线程）
        var stepWorkers = new Dictionary<string, TaskFactory>();
        foreach (var step in _workflowSteps)
        {
            // 为每个步骤创建独立的工作池（核心隔离：避免规则引擎阻塞转发）
            //stepWorkers[step.StepIdentifier] = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(10)); // 最多10个并发
            stepWorkers[step.StepIdentifier] = new TaskFactory(); 
        }

        await foreach (var context in _messageChannel.Reader.ReadAllAsync(token))
        {
            // 异步处理单条消息（避免阻塞消费循环）
            _ = ProcessSingleMessageAsync(context, stepWorkers, token);
        }
    }

    // 单条消息处理流程：上下文构建→解析→并行执行步骤
    private async Task ProcessSingleMessageAsync(
        MqttMessageContext context,
        Dictionary<string, TaskFactory> stepWorkers,
        CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // ========== 第一步：串行解析（必须先解析，才能执行后续步骤） ==========
            _logger.LogInformation("Start processing message, TraceId: {TraceId}, Topic: {Topic}", context.TraceId, context.Topic);
            //_metrics.IncrementCounter("mqtt.message.processing");

            // 选择匹配的解析器
            var parser = _parsers.FirstOrDefault(p => p.Match(context));
            if (parser == null)
            {
                throw new InvalidOperationException($"No parser matched for ProductKey: {context.ProductKey}, TraceId: {context.TraceId}");
            }

            // 执行解析
            await parser.ParseAsync(context);

            _logger.LogInformation("Message parsed successfully, TraceId: {TraceId}, ParseType: {ParseType}", context.TraceId, context.ParseType);

            // ========== 第二步：并行执行所有工作流步骤（核心） ==========
            var stepTasks = new List<Task>();
            foreach (var step in _workflowSteps)
            {
                // 用步骤专属的工作池执行（隔离线程）
                stepTasks.Add(stepWorkers[step.StepIdentifier].StartNew(async () =>
                {
                    try
                    {
                        await step.ExecuteAsync(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Step {StepIdentifier} failed, TraceId: {TraceId}", step.StepIdentifier, context.TraceId);
                        //_metrics.IncrementCounter($"mqtt.step.failed.{step.StepName}");
                    }
                }, token).Unwrap());
            }

            // 等待所有并行步骤完成（失败不阻塞，仅记录状态）
            await Task.WhenAll(stepTasks);

            // ========== 第三步：结果汇总 ==========
            stopwatch.Stop();
            //if (context.IsSuccess)
            //{
            //    _logger.LogInformation("Message processed successfully, TraceId: {TraceId}, TotalElapsed: {Elapsed}ms, Steps: {Steps}",
            //        context.TraceId, stopwatch.ElapsedMilliseconds, JsonSerializer.Serialize(context.StepResults));
            //    _metrics.IncrementCounter("mqtt.message.process.success");
            //}
            //else
            //{
            //    _logger.LogWarning("Message processed with errors, TraceId: {TraceId}, TotalElapsed: {Elapsed}ms, Errors: {Errors}",
            //        context.TraceId, stopwatch.ElapsedMilliseconds, JsonSerializer.Serialize(context.StepResults));
            //    _metrics.IncrementCounter("mqtt.message.process.failed");
            //}

            // 汇总结果：输出结构化日志
            var logDict = context.ToLogDictionary();
            _logger.LogInformation("Mqtt message processed, TraceId: {TraceId}, Result: {@Result}", context.TraceId, logDict);

            if (context.IsOverallSuccess)
            {
               // _metrics.IncrementCounter("mqtt.message.process.success");
            }else
            {
               //_metrics.IncrementCounter("mqtt.message.process.failed");
            }
        } 
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.SetGlobalException(ex);
            _logger.LogError(ex, "Failed to process message, TraceId: {TraceId}, TotalElapsed: {Elapsed}ms",
                context.TraceId, stopwatch.ElapsedMilliseconds);

            //_metrics.IncrementCounter("mqtt.message.process.exception");
            //_metrics.RecordHistogram("mqtt.message.process.elapsed", stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            // 销毁上下文（释放资源）
            context.Dispose();
        }
    }

    // 辅助方法：从Topic解析ProductKey
    private string ParseProductKeyFromTopic(string topic)
    {
        // 示例：Topic格式 /product/abc123/device/def456/upload → 提取abc123
        var parts = topic.Split('/');
        var productKeyIndex = Array.IndexOf(parts, "product") + 1;
        return parts.Length > productKeyIndex ? parts[productKeyIndex] : string.Empty;
    }

    // 辅助方法：从Topic解析DeviceName
    private string ParseDeviceKeyFromTopic(string topic)
    {
        var parts = topic.Split('/');
        var deviceKeyIndex = Array.IndexOf(parts, "device") + 1;
        return parts.Length > deviceKeyIndex ? parts[deviceKeyIndex] : string.Empty;
    }

    #endregion
}
