using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Configurations;
using Artizan.IoT.BatchProcessing.Enums;
using Artizan.IoT.BatchProcessing.Fallbacks;
using Artizan.IoT.BatchProcessing.Health;
using Artizan.IoT.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Core;

/// <summary>
/// 批处理核心基类
/// 【设计思路】：模板方法模式，封装批处理的通用流程，子类仅需实现业务逻辑
/// 【设计考量】：
/// 1. 整合所有策略（隔离/熔断/重试/降级/执行顺序），解耦核心流程和策略实现
/// 2. 线程安全：使用ConcurrentDictionary/ConcurrentQueue保证高并发安全
/// 3. 资源管理：对象池复用、取消令牌管理、资源释放
/// 4. 容错机制：幂等校验、熔断、降级、兜底存储，层层保障数据不丢失
/// 【设计模式】：模板方法模式（Template Method Pattern）
/// </summary>
/// <typeparam name="TMessage">消息类型</typeparam>
public abstract class BatchProcessorBase<TMessage> : IBatchProcessor<TMessage>, IDisposable
{
    #region 依赖注入字段（构造函数注入，遵循依赖注入原则）
    /// <summary>
    /// 批处理核心配置
    /// </summary>
    protected readonly BatchProcessingOptions _batchOptions;

    /// <summary>
    /// 日志器（用于记录核心流程日志）
    /// </summary>
    protected readonly ILogger<BatchProcessorBase<TMessage>> _logger;

    /// <summary>
    /// 分区策略（消息分区计算）
    /// </summary>
    protected readonly IPartitionStrategy _partitionStrategy;

    /// <summary>
    /// 隔离策略（控制分区并发）
    /// </summary>
    protected readonly IIsolateStrategy _isolateStrategy;

    /// <summary>
    /// 熔断策略（防止失败扩散）
    /// </summary>
    protected readonly ICircuitBreakerStrategy _circuitBreakerStrategy;

    /// <summary>
    /// 重试策略（失败自动重试）
    /// </summary>
    protected readonly IRetryStrategy _retryStrategy;

    /// <summary>
    /// 降级策略（服务降级）
    /// </summary>
    protected readonly IDegradeStrategy _degradeStrategy;

    /// <summary>
    /// 执行顺序策略（串行/并行控制）
    /// </summary>
    protected readonly IExecutionOrderStrategy _executionOrderStrategy;

    /// <summary>
    /// 幂等性校验器（防止重复处理）
    /// </summary>
    protected readonly IIdempotentChecker _idempotentChecker;

    /// <summary>
    /// 兜底存储工厂（创建不同类型的兜底存储）
    /// </summary>
    protected readonly BatchFallbackStoreFactory _fallbackStoreFactory;

    /// <summary>
    /// 健康检查器（监控分区状态）
    /// </summary>
    protected readonly PartitionHealthChecker _healthChecker;

    /// <summary>
    /// 分区调度器（动态扩缩容）
    /// </summary>
    protected readonly PartitionDispatcher _partitionDispatcher;
    #endregion

    #region 内部状态（线程安全集合，保证高并发安全）
    /// <summary>
    /// 分区消息队列（每个分区独立队列，避免跨分区竞争）
    /// </summary>
    protected readonly ConcurrentDictionary<string, ConcurrentQueue<TimedMessage<TMessage>>> _partitionQueues =
        new ConcurrentDictionary<string, ConcurrentQueue<TimedMessage<TMessage>>>();

    /// <summary>
    /// 批处理任务取消令牌源（统一管理所有任务的取消）
    /// </summary>
    protected readonly CancellationTokenSource _processingCts = new CancellationTokenSource();

    /// <summary>
    /// 分区处理任务字典（每个分区一个处理任务，便于监控和重启）
    /// </summary>
    protected readonly ConcurrentDictionary<string, Task> _processingTasks = new ConcurrentDictionary<string, Task>();

    /// <summary>
    /// 批处理大小（每批处理的消息数量）
    /// </summary>
    protected readonly int _batchSize;

    /// <summary>
    /// 批处理间隔（控制处理频率，避免过度占用CPU）
    /// </summary>
    protected readonly TimeSpan _batchInterval;

    /// <summary>
    /// 资源释放标记（防止重复释放）
    /// </summary>
    private bool _disposed = false;
    #endregion

    /// <summary>
    /// 构造函数（依赖注入）
    /// 【设计思路】：构造函数注入所有依赖，遵循「依赖倒置原则」
    /// 【设计考量】：
    /// 1. 防御性编程：校验所有依赖不为null
    /// 2. 初始化批处理参数和分区队列
    /// 3. 启动所有分区的处理任务
    /// </summary>
    /// <param name="batchOptions">批处理配置</param>
    /// <param name="logger">日志器</param>
    /// <param name="partitionStrategy">分区策略</param>
    /// <param name="isolateStrategy">隔离策略</param>
    /// <param name="circuitBreakerStrategy">熔断策略</param>
    /// <param name="retryStrategy">重试策略</param>
    /// <param name="degradeStrategy">降级策略</param>
    /// <param name="executionOrderStrategy">执行顺序策略</param>
    /// <param name="idempotentChecker">幂等校验器</param>
    /// <param name="fallbackStoreFactory">兜底存储工厂</param>
    /// <param name="healthChecker">健康检查器</param>
    /// <param name="partitionDispatcher">分区调度器</param>
    protected BatchProcessorBase(
        IOptions<BatchProcessingOptions> batchOptions,
        ILogger<BatchProcessorBase<TMessage>> logger,
        IPartitionStrategy partitionStrategy,
        IIsolateStrategy isolateStrategy,
        ICircuitBreakerStrategy circuitBreakerStrategy,
        IRetryStrategy retryStrategy,
        IDegradeStrategy degradeStrategy,
        IExecutionOrderStrategy executionOrderStrategy,
        IIdempotentChecker idempotentChecker,
        BatchFallbackStoreFactory fallbackStoreFactory,
        PartitionHealthChecker healthChecker,
        PartitionDispatcher partitionDispatcher)
    {
        // 防御性编程：校验所有依赖不为null
        _batchOptions = batchOptions?.Value ?? throw new ArgumentNullException(nameof(batchOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _partitionStrategy = partitionStrategy ?? throw new ArgumentNullException(nameof(partitionStrategy));
        _isolateStrategy = isolateStrategy ?? throw new ArgumentNullException(nameof(isolateStrategy));
        _circuitBreakerStrategy = circuitBreakerStrategy ?? throw new ArgumentNullException(nameof(circuitBreakerStrategy));
        _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
        _degradeStrategy = degradeStrategy ?? throw new ArgumentNullException(nameof(degradeStrategy));
        _executionOrderStrategy = executionOrderStrategy ?? throw new ArgumentNullException(nameof(executionOrderStrategy));
        _idempotentChecker = idempotentChecker ?? throw new ArgumentNullException(nameof(idempotentChecker));
        _fallbackStoreFactory = fallbackStoreFactory ?? throw new ArgumentNullException(nameof(fallbackStoreFactory));
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
        _partitionDispatcher = partitionDispatcher ?? throw new ArgumentNullException(nameof(partitionDispatcher));

        // 初始化批处理参数
        _batchSize = _batchOptions.BatchSize;
        _batchInterval = _batchOptions.BatchInterval;

        // 初始化所有分区队列
        for (int i = 0; i < _batchOptions.PartitionCount; i++)
        {
            var partitionKey = $"partition_{i}";
            _partitionQueues.TryAdd(partitionKey, new ConcurrentQueue<TimedMessage<TMessage>>());
        }

        // 启动所有分区的处理任务
        StartAllPartitionProcessingTasks();

        _logger.LogInformation(
            "[TraceId:None] 批处理基类初始化完成 [消息类型:{MessageType}, 初始分区数:{PartitionCount}, 批大小:{BatchSize}, 批间隔:{BatchInterval}ms]",
            typeof(TMessage).FullName,
            _batchOptions.PartitionCount,
            _batchSize,
            _batchInterval.TotalMilliseconds);
    }

    #region 核心接口实现：消息入队（生产端核心逻辑）
    /// <summary>
    /// 消息入队（模板方法的前置流程）
    /// 【处理流程】：
    /// 1. 幂等性校验 → 2. 熔断检查 → 3. 隔离策略检查 → 4. 对象池获取 → 5. 入队 → 6. 健康状态更新
    /// 【容错设计】：任何步骤失败都将消息存入兜底存储，保证数据不丢失
    /// </summary>
    /// <param name="message">待入队消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>入队结果</returns>
    public async Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        // 代码规范：即使单行也用{}
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message), "消息不能为空");
        }

        var traceId = TraceIdGenerator.Generate();
        string messageId = null;

        try
        {
            // 1. 生成消息ID并做幂等性校验（防止重复入队）
            messageId = GetMessageId(message);
            if (await _idempotentChecker.CheckAsync(messageId, traceId, cancellationToken))
            {
                _logger.LogDebug(
                    "[TraceId:{TraceId}] 消息已处理，跳过入队 [MessageId:{MessageId}, MessageType:{MessageType}]",
                    traceId,
                    messageId,
                    typeof(TMessage).FullName);
                return;
            }

            // 2. 计算消息所属分区
            var partitionKey = _partitionStrategy.GetPartitionKey(message, _partitionDispatcher.CurrentPartitionCount);

            // 3. 检查分区熔断器是否打开（快速失败）
            if (_circuitBreakerStrategy.IsOpen(partitionKey))
            {
                _logger.LogWarning(
                    "[TraceId:{TraceId}] 分区熔断器打开，消息进入兜底存储 [PartitionKey:{PartitionKey}, MessageId:{MessageId}]",
                    traceId,
                    partitionKey,
                    messageId);

                // 熔断时直接存入兜底存储
                var fallbackStore = _fallbackStoreFactory.CreateStore<TMessage>();
                await fallbackStore.StoreAsync(
                    message,
                    "分区熔断器打开，快速失败",
                    FallbackType.CircuitBreakerFailure,
                    traceId,
                    messageId,
                    cancellationToken);
                return;
            }

            // 4. 尝试获取隔离许可（控制并发）
            if (!await _isolateStrategy.TryEnterAsync(partitionKey, _batchOptions.IsolateMaxConcurrencyPerPartition, cancellationToken))
            {
                _logger.LogWarning(
                    "[TraceId:{TraceId}] 分区并发数超限，消息进入兜底存储 [PartitionKey:{PartitionKey}, MessageId:{MessageId}]",
                    traceId,
                    partitionKey,
                    messageId);

                // 并发超限时存入兜底存储
                var fallbackStore = _fallbackStoreFactory.CreateStore<TMessage>();
                await fallbackStore.StoreAsync(
                    message,
                    $"分区并发数超限（最大:{_batchOptions.IsolateMaxConcurrencyPerPartition}）",
                    FallbackType.IsolateFailure,
                    traceId,
                    messageId,
                    cancellationToken);
                return;
            }

            // 5. 从对象池获取消息对象并初始化
            var timedMessage = TimedMessagePool<TMessage>.Get(message, traceId, messageId);

            // 6. 入队到分区队列
            var queue = _partitionQueues.GetOrAdd(partitionKey, _ => new ConcurrentQueue<TimedMessage<TMessage>>());
            queue.Enqueue(timedMessage);

            // 7. 更新健康检查状态
            _healthChecker.UpdatePartitionStatus(
                partitionKey,
                queue.Count,
                0,
                0,
                GetPartitionExecutionMode(partitionKey),
                _isolateStrategy.GetCurrentConcurrency(partitionKey));

            _logger.LogDebug(
                "[TraceId:{TraceId}] 消息入队成功 [PartitionKey:{PartitionKey}, MessageId:{MessageId}, QueueLength:{QueueLength}]",
                traceId,
                partitionKey,
                messageId,
                queue.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 消息入队失败 [MessageId:{MessageId}, MessageType:{MessageType}]",
                traceId,
                messageId,
                typeof(TMessage).FullName);

            // 最终兜底：入队失败时存储消息
            if (!string.IsNullOrEmpty(messageId))
            {
                var fallbackStore = _fallbackStoreFactory.CreateStore<TMessage>();
                await fallbackStore.StoreAsync(
                    message,
                    $"入队失败：{ex.Message}",
                    FallbackType.EnqueueFailure,
                    traceId,
                    messageId,
                    cancellationToken);
            }
        }
    }
    #endregion

    #region 核心接口实现：批处理执行（抽象方法，子类实现业务逻辑）
    /// <summary>
    /// 执行批处理（消费端核心逻辑）
    /// 【设计思路】：模板方法的钩子方法（Hook Method），子类实现具体业务逻辑
    /// 【设计考量】：子类仅需关注业务逻辑，通用流程由基类封装
    /// </summary>
    /// <param name="messages">批量消息列表</param>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    public abstract Task ProcessBatchAsync(
        List<TMessage> messages,
        string partitionKey,
        string traceId,
        CancellationToken cancellationToken = default);
    #endregion

    #region 核心接口实现：切换分区执行模式
    /// <summary>
    /// 切换分区执行模式（串行/并行）
    /// 【设计考量】：
    /// 1. 完整的日志记录，便于问题排查
    /// 2. 异常处理：保证切换失败时返回明确结果
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="newMode">新执行模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换是否成功</returns>
    public async Task<bool> SwitchPartitionExecutionModeAsync(
        string partitionKey,
        ExecutionMode newMode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        var traceId = TraceIdGenerator.Generate();
        _logger.LogInformation(
            "[TraceId:{TraceId}] 尝试切换分区执行模式 [PartitionKey:{PartitionKey}, NewMode:{NewMode}]",
            traceId,
            partitionKey,
            newMode);

        try
        {
            var success = await _executionOrderStrategy.ChangeExecutionModeAsync(
                partitionKey,
                newMode,
                _batchOptions.ExecutionModeSwitchTimeout,
                cancellationToken);

            if (success)
            {
                // 更新配置中的执行模式
                _batchOptions.PartitionExecutionModes[partitionKey] = newMode;
                _logger.LogInformation(
                    "[TraceId:{TraceId}] 分区执行模式切换成功 [PartitionKey:{PartitionKey}, NewMode:{NewMode}]",
                    traceId,
                    partitionKey,
                    newMode);
            }
            else
            {
                _logger.LogError(
                    "[TraceId:{TraceId}] 分区执行模式切换超时 [PartitionKey:{PartitionKey}, NewMode:{NewMode}, Timeout:{Timeout}s]",
                    traceId,
                    partitionKey,
                    newMode,
                    _batchOptions.ExecutionModeSwitchTimeout.TotalSeconds);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 分区执行模式切换失败 [PartitionKey:{PartitionKey}, NewMode:{NewMode}]",
                traceId,
                partitionKey,
                newMode);
            return false;
        }
    }
    #endregion

    #region 内部核心方法：启动/管理分区处理任务
    /// <summary>
    /// 启动所有分区的处理任务
    /// 【设计考量】：每个分区独立任务，避免单分区故障影响整体服务
    /// </summary>
    protected void StartAllPartitionProcessingTasks()
    {
        foreach (var partitionKey in _partitionQueues.Keys)
        {
            StartPartitionProcessingTask(partitionKey);
        }
    }

    /// <summary>
    /// 启动单个分区的处理任务
    /// 【设计考量】：
    /// 1. 任务异常时自动重启，保证高可用
    /// 2. 取消令牌管理，支持优雅退出
    /// 3. 异常延迟重试，避免频繁报错
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    protected void StartPartitionProcessingTask(string partitionKey)
    {
        if (_processingTasks.ContainsKey(partitionKey))
        {
            return;
        }

        // 启动分区处理任务
        var task = Task.Run(async () =>
        {
            var traceId = TraceIdGenerator.Generate();
            _logger.LogInformation(
                "[TraceId:{TraceId}] 分区处理任务启动 [PartitionKey:{PartitionKey}]",
                traceId,
                partitionKey);

            while (!_processingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await ProcessPartitionQueueAsync(partitionKey, _processingCts.Token);
                    await Task.Delay(_batchInterval, _processingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(
                        "[TraceId:{TraceId}] 分区处理任务已取消 [PartitionKey:{PartitionKey}]",
                        traceId,
                        partitionKey);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[TraceId:{TraceId}] 分区处理任务异常 [PartitionKey:{PartitionKey}]",
                        traceId,
                        partitionKey);
                    // 异常后延迟1秒重试，避免频繁报错
                    await Task.Delay(1000, _processingCts.Token);
                }
            }
        }, _processingCts.Token);

        _processingTasks[partitionKey] = task;

        // 任务完成后清理 + 异常重启
        task.ContinueWith(t =>
        {
            _processingTasks.TryRemove(partitionKey, out _);

            if (t.Exception != null)
            {
                _logger.LogError(
                    t.Exception,
                    "[TraceId:None] 分区处理任务异常终止 [PartitionKey:{PartitionKey}]",
                    partitionKey);

                // 非取消状态下重启任务
                if (!_processingCts.Token.IsCancellationRequested)
                {
                    StartPartitionProcessingTask(partitionKey);
                }
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// 处理单个分区的消息队列（核心消费逻辑）
    /// 【处理流程】：
    /// 1. 出队批量消息 → 2. 重试策略执行 → 3. 执行顺序策略 → 4. 业务处理 → 5. 结果处理（成功/失败）
    /// 【设计考量】：
    /// 1. 完整的异常处理和资源释放
    /// 2. 对象池归还，减少GC压力
    /// 3. 健康状态更新，便于监控
    /// 4. 失败时触发熔断、降级、兜底存储
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    protected async Task ProcessPartitionQueueAsync(string partitionKey, CancellationToken cancellationToken)
    {
        var queue = _partitionQueues.GetOrAdd(partitionKey, _ => new ConcurrentQueue<TimedMessage<TMessage>>());
        var batchMessages = new List<TMessage>();
        var batchTimedMessages = new List<TimedMessage<TMessage>>();
        var traceId = TraceIdGenerator.Generate();

        try
        {
            // 1. 从队列中取出批量消息（最多_batchSize条）
            while (queue.TryDequeue(out var timedMessage) && batchMessages.Count < _batchSize)
            {
                batchMessages.Add(timedMessage.Payload);
                batchTimedMessages.Add(timedMessage);
            }

            // 无消息时直接返回
            if (batchMessages.Count == 0)
            {
                return;
            }

            _logger.LogDebug(
                "[TraceId:{TraceId}] 开始处理分区批消息 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                traceId,
                partitionKey,
                batchMessages.Count);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int failureCount = 0;

            try
            {
                // 2. 执行重试策略
                await _retryStrategy.ExecuteAsync(async (ct) =>
                {
                    // 3. 执行顺序策略（串行/并行）
                    await _executionOrderStrategy.ExecuteAsync(
                        partitionKey,
                        async (innerCt) =>
                        {
                            // 4. 调用子类实现的业务逻辑
                            await ProcessBatchAsync(batchMessages, partitionKey, traceId, innerCt);
                            return true;
                        },
                        GetPartitionExecutionMode(partitionKey),
                        ct);

                    return true;
                }, partitionKey, _batchOptions.RetryMaxCount, _batchOptions.RetryInterval, cancellationToken);

                // 5. 处理成功：重置熔断状态 + 标记消息已处理
                _circuitBreakerStrategy.RecordSuccess(partitionKey);
                var messageIds = batchTimedMessages.Select(m => m.MessageId).ToList();
                foreach (var messageId in messageIds)
                {
                    await _idempotentChecker.MarkAsProcessedAsync(messageId, traceId, cancellationToken);
                }

                _logger.LogInformation(
                    "[TraceId:{TraceId}] 批处理成功 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}, Elapsed:{Elapsed}ms]",
                    traceId,
                    partitionKey,
                    batchMessages.Count,
                    stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                failureCount = batchMessages.Count;
                _logger.LogError(
                    ex,
                    "[TraceId:{TraceId}] 批处理失败 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                    traceId,
                    partitionKey,
                    batchMessages.Count);

                // 6. 处理失败：记录熔断 + 执行降级 + 兜底存储
                _circuitBreakerStrategy.RecordFailure(partitionKey);
                await _degradeStrategy.ExecuteAsync(partitionKey, batchMessages.Cast<object>().ToList(), traceId, cancellationToken);

                var fallbackStore = _fallbackStoreFactory.CreateStore<TMessage>();
                await fallbackStore.StoreBatchAsync(
                    batchMessages,
                    partitionKey,
                    FallbackType.ProcessFailure,
                    traceId,
                    cancellationToken);

                throw; // 抛出异常，触发重试/任务重启
            }
            finally
            {
                stopwatch.Stop();

                // 7. 释放隔离资源（必须释放，避免资源泄漏）
                _isolateStrategy.Release(partitionKey);

                // 8. 归还对象池资源（减少GC压力）
                foreach (var timedMessage in batchTimedMessages)
                {
                    TimedMessagePool<TMessage>.Return(timedMessage);
                }

                // 9. 更新健康检查状态
                _healthChecker.UpdatePartitionStatus(
                    partitionKey,
                    queue.Count,
                    stopwatch.Elapsed.TotalMilliseconds,
                    failureCount,
                    0,
                    GetPartitionExecutionMode(partitionKey),
                    _isolateStrategy.GetCurrentConcurrency(partitionKey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 处理分区队列异常 [PartitionKey:{PartitionKey}]",
                traceId,
                partitionKey);
        }
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 获取消息唯一ID（钩子方法，子类可重写）
    /// 【设计思路】：
    /// 1. 优先反射获取Id/MessageId属性，适配不同消息类型
    /// 2. 无ID属性时生成GUID，保证唯一性
    /// </summary>
    /// <param name="message">消息对象</param>
    /// <returns>消息唯一ID</returns>
    protected virtual string GetMessageId(TMessage message)
    {
        if (message == null)
        {
            return Guid.NewGuid().ToString("N");
        }

        // 尝试反射获取Id/MessageId属性
        var type = message.GetType();
        var idProperty = type.GetProperty("Id") ?? type.GetProperty("MessageId") ?? type.GetProperty("ID");
        if (idProperty != null && idProperty.CanRead)
        {
            var idValue = idProperty.GetValue(message)?.ToString();
            if (!string.IsNullOrEmpty(idValue))
            {
                return idValue;
            }
        }

        // 无ID属性时生成GUID
        return $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    /// <summary>
    /// 获取分区执行模式
    /// 【设计考量】：优先使用分区自定义模式，无则使用默认模式
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <returns>执行模式</returns>
    protected ExecutionMode GetPartitionExecutionMode(string partitionKey)
    {
        if (_batchOptions.PartitionExecutionModes.TryGetValue(partitionKey, out var mode))
        {
            return mode;
        }

        return _batchOptions.DefaultExecutionMode;
    }
    #endregion

    #region 资源释放（IDisposable实现，遵循资源释放规范）
    /// <summary>
    /// 释放资源
    /// 【设计考量】：
    /// 1. 实现IDisposable接口，释放非托管资源
    /// 2. 取消所有批处理任务，避免资源泄漏
    /// 3. 清理剩余消息到兜底存储，保证数据不丢失
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源（核心实现）
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            var traceId = TraceIdGenerator.Generate();
            _logger.LogInformation("[TraceId:{TraceId}] 开始释放批处理资源", traceId);

            // 1. 取消所有处理任务
            _processingCts.Cancel();

            // 2. 等待所有任务完成（最多5秒）
            try
            {
                Task.WaitAll(_processingTasks.Values.ToArray(), 5000);
            }
            catch (AggregateException ex)
            {
                _logger.LogWarning(ex, "[TraceId:{TraceId}] 等待任务完成时出现异常", traceId);
            }

            // 3. 清理剩余消息到兜底存储
            var fallbackStore = _fallbackStoreFactory.CreateStore<TMessage>();
            foreach (var kvp in _partitionQueues)
            {
                var partitionKey = kvp.Key;
                var queue = kvp.Value;
                var remainingMessages = new List<TMessage>();

                while (queue.TryDequeue(out var timedMessage))
                {
                    remainingMessages.Add(timedMessage.Payload);
                    TimedMessagePool<TMessage>.Return(timedMessage);
                }

                if (remainingMessages.Count > 0)
                {
                    try
                    {
                        fallbackStore.StoreBatchAsync(
                            remainingMessages,
                            partitionKey,
                            FallbackType.ShutdownRemaining,
                            traceId,
                            CancellationToken.None).Wait();

                        _logger.LogInformation(
                            "[TraceId:{TraceId}] 分区剩余消息已存入兜底存储 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                            traceId,
                            partitionKey,
                            remainingMessages.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                        ex,
                        "[TraceId:{TraceId}] 存储剩余消息失败 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                        traceId,
                        partitionKey,
                        remainingMessages.Count);
                    }
                }
            }

            // 4. 释放取消令牌源
            _processingCts.Dispose();

            _logger.LogInformation("[TraceId:{TraceId}] 批处理资源释放完成", traceId);
        }

        _disposed = true;
    }

    /// <summary>
    /// 析构函数（防止未手动释放资源）
    /// </summary>
    ~BatchProcessorBase()
    {
        Dispose(false);
    }
    #endregion
}