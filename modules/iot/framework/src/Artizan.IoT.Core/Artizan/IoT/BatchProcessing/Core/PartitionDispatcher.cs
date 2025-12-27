using Artizan.IoT.BatchProcessing.Configurations;
using Artizan.IoT.BatchProcessing.Health;
using Artizan.IoT.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Core;

/// <summary>
/// 分区调度器（动态扩缩容）
/// 【设计思路】：根据分区队列负载动态调整分区数量，优化资源利用率
/// 【设计考量】：
/// 1. 基于平均队列长度触发扩缩容，避免资源浪费/过载
/// 2. 扩缩容间隔控制，避免频繁调整
/// 3. 扩缩容时平滑迁移队列，不丢失消息
/// 【设计模式】：单例模式（逻辑单例，依赖注入管理）+ 观察者模式（监听健康状态）
/// </summary>
public class PartitionDispatcher : IDisposable
{
    /// <summary>
    /// 批处理配置
    /// </summary>
    private readonly BatchProcessingOptions _options;

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<PartitionDispatcher> _logger;

    /// <summary>
    /// 健康检查器
    /// </summary>
    private readonly PartitionHealthChecker _healthChecker;

    /// <summary>
    /// 调度任务取消令牌源
    /// </summary>
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    /// <summary>
    /// 调度后台任务
    /// </summary>
    private readonly Task _dispatchTask;

    /// <summary>
    /// 当前分区数
    /// </summary>
    private int _currentPartitionCount;

    /// <summary>
    /// 上次调整时间（避免频繁调整）
    /// </summary>
    private DateTime _lastAdjustTime = DateTime.UtcNow;

    /// <summary>
    /// 资源释放标记
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 当前分区数（线程安全访问）
    /// </summary>
    public int CurrentPartitionCount
    {
        get => Interlocked.CompareExchange(ref _currentPartitionCount, 0, 0);
        private set => Interlocked.Exchange(ref _currentPartitionCount, value);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">批处理配置</param>
    /// <param name="logger">日志器</param>
    /// <param name="healthChecker">健康检查器</param>
    public PartitionDispatcher(
        IOptions<BatchProcessingOptions> options,
        ILogger<PartitionDispatcher> logger,
        PartitionHealthChecker healthChecker)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));

        // 初始化当前分区数
        CurrentPartitionCount = _options.PartitionCount;

        // 启动调度后台任务
        _dispatchTask = Task.Run(async () => await DispatchLoopAsync(_cts.Token), _cts.Token);

        _logger.LogInformation(
            "[TraceId:None] 分区调度器初始化完成 [初始分区数:{PartitionCount}, 扩容阈值:{ExpandThreshold}, 缩容阈值:{ShrinkThreshold}]",
            CurrentPartitionCount,
            _options.PartitionExpandThreshold,
            _options.PartitionShrinkThreshold);
    }

    /// <summary>
    /// 调度循环（核心逻辑）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private async Task DispatchLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 检查是否需要调整分区数
                await CheckAndAdjustPartitionsAsync(cancellationToken);

                // 按配置间隔等待
                await Task.Delay(_options.PartitionAdjustInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[TraceId:None] 分区调度循环已取消");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId:None] 分区调度循环异常");
                await Task.Delay(1000, cancellationToken); // 异常后延迟重试
            }
        }
    }

    /// <summary>
    /// 检查并调整分区数
    /// 【核心逻辑】：
    /// 1. 检查调整间隔（避免频繁调整）
    /// 2. 计算平均队列长度
    /// 3. 扩容：平均队列 > 扩容阈值 且 当前分区 < 最大分区数
    /// 4. 缩容：平均队列 < 缩容阈值 且 当前分区 > 最小分区数
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private async Task CheckAndAdjustPartitionsAsync(CancellationToken cancellationToken)
    {
        var traceId = TraceIdGenerator.Generate();

        // 1. 检查调整间隔（至少间隔配置的调整时间）
        var now = DateTime.UtcNow;
        if ((now - _lastAdjustTime) < _options.PartitionAdjustInterval)
        {
            return;
        }

        // 2. 获取所有分区健康状态
        var partitionStatuses = _healthChecker.GetAllPartitionStatuses();
        if (partitionStatuses.Count == 0)
        {
            _logger.LogDebug("[TraceId:{TraceId}] 无分区状态数据，跳过调整", traceId);
            return;
        }

        // 3. 计算平均队列长度
        var avgQueueLength = partitionStatuses.Average(s => s.QueueLength);
        _logger.LogDebug(
            "[TraceId:{TraceId}] 分区负载检查 [当前分区数:{CurrentCount}, 平均队列长度:{AvgQueueLength}, 扩容阈值:{ExpandThreshold}, 缩容阈值:{ShrinkThreshold}]",
            traceId,
            CurrentPartitionCount,
            avgQueueLength,
            _options.PartitionExpandThreshold,
            _options.PartitionShrinkThreshold);

        // 4. 判断是否需要扩容
        if (avgQueueLength > _options.PartitionExpandThreshold && CurrentPartitionCount < _options.MaxPartitionCount)
        {
            // 扩容策略：每次扩容50%（向上取整），不超过最大分区数
            var newCount = Math.Min((int)Math.Ceiling(CurrentPartitionCount * 1.5), _options.MaxPartitionCount);
            await ExpandPartitionsAsync(newCount, traceId, cancellationToken);
            _lastAdjustTime = now;
        }
        // 5. 判断是否需要缩容
        else if (avgQueueLength < _options.PartitionShrinkThreshold && CurrentPartitionCount > _options.MinPartitionCount)
        {
            // 缩容策略：每次缩容到当前的2/3（向下取整），不低于最小分区数
            var newCount = Math.Max((int)Math.Floor(CurrentPartitionCount * 2.0 / 3), _options.MinPartitionCount);
            await ShrinkPartitionsAsync(newCount, traceId, cancellationToken);
            _lastAdjustTime = now;
        }
    }

    /// <summary>
    /// 扩容分区
    /// </summary>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private async Task ExpandPartitionsAsync(int newCount, string traceId, CancellationToken cancellationToken)
    {
        if (newCount <= CurrentPartitionCount)
        {
            _logger.LogWarning("[TraceId:{TraceId}] 新分区数不大于当前数，跳过扩容 [当前:{Current}, 新值:{New}]", traceId, CurrentPartitionCount, newCount);
            return;
        }

        _logger.LogInformation(
            "[TraceId:{TraceId}] 开始扩容分区 [当前:{Current}, 新值:{New}, 最大:{Max}]",
            traceId,
            CurrentPartitionCount,
            newCount,
            _options.MaxPartitionCount);

        try
        {
            // 更新当前分区数
            CurrentPartitionCount = newCount;

            // 通知健康检查器更新分区数
            _healthChecker.UpdatePartitionCount(newCount);

            _logger.LogInformation(
                "[TraceId:{TraceId}] 分区扩容完成 [当前:{Current}]",
                traceId,
                CurrentPartitionCount);

            // 触发告警（如果启用）
            if (_options.EnablePartitionAlarm)
            {
                await TriggerPartitionAdjustAlarmAsync("扩容", CurrentPartitionCount, traceId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 分区扩容失败 [当前:{Current}, 新值:{New}]",
                traceId,
                CurrentPartitionCount,
                newCount);
            throw;
        }
    }

    /// <summary>
    /// 缩容分区
    /// </summary>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private async Task ShrinkPartitionsAsync(int newCount, string traceId, CancellationToken cancellationToken)
    {
        if (newCount >= CurrentPartitionCount)
        {
            _logger.LogWarning("[TraceId:{TraceId}] 新分区数不小于当前数，跳过缩容 [当前:{Current}, 新值:{New}]", traceId, CurrentPartitionCount, newCount);
            return;
        }

        _logger.LogInformation(
            "[TraceId:{TraceId}] 开始缩容分区 [当前:{Current}, 新值:{New}, 最小:{Min}]",
            traceId,
            CurrentPartitionCount,
            newCount,
            _options.MinPartitionCount);

        try
        {
            // 更新当前分区数
            CurrentPartitionCount = newCount;

            // 通知健康检查器更新分区数
            _healthChecker.UpdatePartitionCount(newCount);

            _logger.LogInformation(
                "[TraceId:{TraceId}] 分区缩容完成 [当前:{Current}]",
                traceId,
                CurrentPartitionCount);

            // 触发告警（如果启用）
            if (_options.EnablePartitionAlarm)
            {
                await TriggerPartitionAdjustAlarmAsync("缩容", CurrentPartitionCount, traceId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 分区缩容失败 [当前:{Current}, 新值:{New}]",
                traceId,
                CurrentPartitionCount,
                newCount);
            throw;
        }
    }

    /// <summary>
    /// 触发分区调整告警（钩子方法，可扩展）
    /// </summary>
    /// <param name="adjustType">调整类型（扩容/缩容）</param>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task TriggerPartitionAdjustAlarmAsync(string adjustType, int newCount, string traceId, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "[TraceId:{TraceId}] 分区{AdjustType}告警 [新分区数:{NewCount}]",
            traceId,
            adjustType,
            newCount);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放资源
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
            // 取消调度任务
            _cts.Cancel();

            // 等待任务完成
            try
            {
                _dispatchTask.Wait(1000);
            }
            catch (AggregateException ex)
            {
                _logger.LogWarning(ex, "[TraceId:None] 等待调度任务完成时出现异常");
            }

            // 释放取消令牌源
            _cts.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~PartitionDispatcher()
    {
        Dispose(false);
    }
}