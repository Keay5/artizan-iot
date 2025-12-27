using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Configurations;
using Artizan.IoT.BatchProcessing.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Artizan.IoT.BatchProcessing.Health;

/// <summary>
/// 分区健康检查器（核心健康监控组件）
/// 【设计思路】：
/// 1. 实时收集各分区健康指标（并发、失败、队列、熔断）
/// 2. 自动评估健康等级，支持告警和自动处理
/// 3. 线程安全：基于ConcurrentDictionary存储状态，无锁设计
/// 4. 可配置阈值：通过Options调整健康判定规则
/// 【设计模式】：单例模式（全局唯一）+ 观察者模式（指标更新）
/// </summary>
public class PartitionHealthChecker : IDisposable
{
    #region 依赖与配置
    private readonly ILogger<PartitionHealthChecker> _logger;
    private readonly ICircuitBreakerStrategy _circuitBreakerStrategy;
    private readonly IIsolateStrategy _isolateStrategy;
    private readonly BatchProcessingOptions _options;

    // 存储所有分区的健康状态（线程安全）
    private readonly ConcurrentDictionary<string, PartitionHealthStatus> _partitionHealthDict = new();

    // 失败计数滑动窗口（按分钟统计，避免瞬时值影响）
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _failureTimeWindowDict = new();

    // 定时清理过期失败记录（每分钟执行）
    private readonly Timer _cleanupTimer;

    // 健康检查阈值配置（可通过Options覆盖）
    private readonly double _warningFailureRateThreshold = 0.1; // 警告：失败率≥10%
    private readonly double _abnormalFailureRateThreshold = 0.3; // 异常：失败率≥30%
    private readonly int _warningQueueLengthThreshold = 500; // 警告：队列长度≥500
    private readonly int _abnormalQueueLengthThreshold = 1000; // 异常：队列长度≥1000

    // 当前全局分区总数（动态更新）
    private int _currentPartitionCount;
    #endregion

    #region 构造函数（核心修正：变量名替换）
    /// <summary>
    /// 构造函数（依赖注入）
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="circuitBreakerStrategy">熔断策略</param>
    /// <param name="isolateStrategy">隔离策略</param>
    /// <param name="options">批处理配置</param>
    public PartitionHealthChecker(
        ILogger<PartitionHealthChecker> logger,
        ICircuitBreakerStrategy circuitBreakerStrategy,
        IIsolateStrategy isolateStrategy,
        IOptions<BatchProcessingOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreakerStrategy = circuitBreakerStrategy ?? throw new ArgumentNullException(nameof(circuitBreakerStrategy));
        _isolateStrategy = isolateStrategy ?? throw new ArgumentNullException(nameof(isolateStrategy));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // 初始化清理定时器（每分钟清理1分钟前的失败记录）
        _cleanupTimer = new Timer(CleanupExpiredFailures, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        // 从配置覆盖阈值
        if (_options.PartitionExpandThreshold > 0)
        {
            _warningQueueLengthThreshold = _options.PartitionExpandThreshold / 2;
            _abnormalQueueLengthThreshold = _options.PartitionExpandThreshold;
        }

        // 初始化分区总数为配置的初始值
        _currentPartitionCount = _options.PartitionCount;

        _logger.LogInformation("分区健康检查器初始化完成，警告队列阈值：{WarningQueue}，异常失败率阈值：{AbnormalFailureRate}%，当前分区总数: {CurrentPartitionCount}, 默认分区最大并发数：{MaxConcurrency}",
            _warningQueueLengthThreshold,
            _abnormalFailureRateThreshold * 100,
            _currentPartitionCount,
            _options.IsolateMaxConcurrencyPerPartition);
    }
    #endregion

    #region 核心监控方法
    /// <summary>
    /// 初始化分区健康状态（首次使用分区时调用）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="maxConcurrency">最大并发数（默认用配置值）</param>
    public void InitializePartition(string partitionKey, int maxConcurrency = 0)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 懒加载初始化分区状态
        _partitionHealthDict.GetOrAdd(partitionKey, key => new PartitionHealthStatus
        {
            PartitionKey = key,
            MaxConcurrency = maxConcurrency > 0 ? maxConcurrency : _options.IsolateMaxConcurrencyPerPartition
        });

        // 初始化失败时间窗口
        _failureTimeWindowDict.GetOrAdd(partitionKey, _ => new Queue<DateTime>());

        _logger.LogDebug("[TraceId:None] 初始化分区健康状态 [PartitionKey:{PartitionKey}, MaxConcurrency:{MaxConcurrency}]",
            partitionKey, maxConcurrency > 0 ? maxConcurrency : _options.IsolateMaxConcurrencyPerPartition);
    }

    /// <summary>
    /// 记录分区处理失败（更新失败指标）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="traceId">追踪ID</param>
    public void RecordFailure(string partitionKey, string traceId)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 线程安全添加失败时间戳
        lock (_failureTimeWindowDict[partitionKey]) // 队列操作需加锁（Queue非线程安全）
        {
            _failureTimeWindowDict[partitionKey].Enqueue(DateTime.UtcNow);
        }

        // 更新失败指标并评估健康等级
        UpdateFailureMetrics(partitionKey);
        EvaluateHealthLevel(partitionKey);

        _logger.LogWarning("[TraceId:{TraceId}] 记录分区处理失败 [PartitionKey:{PartitionKey}, 当前失败数（1分钟）:{FailureCount}]",
            traceId, partitionKey, _failureTimeWindowDict[partitionKey].Count);
    }

    /// <summary>
    /// 记录分区处理成功（更新成功时间）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    public void RecordSuccess(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 更新成功时间
        _partitionHealthDict[partitionKey].UpdateSuccessTime();

        // 评估健康等级（成功可能让等级恢复）
        EvaluateHealthLevel(partitionKey);
    }

    /// <summary>
    /// 更新分区队列长度
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="queueLength">当前队列长度</param>
    public void UpdateQueueLength(string partitionKey, int queueLength)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 更新队列长度
        _partitionHealthDict[partitionKey].UpdateQueueLength(queueLength);

        // 评估健康等级
        EvaluateHealthLevel(partitionKey);

        _logger.LogDebug("[TraceId:None] 更新分区队列长度 [PartitionKey:{PartitionKey}, QueueLength:{QueueLength}]",
            partitionKey, queueLength);
    }

    /// <summary>
    /// 更新分区并发数
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="currentConcurrency">当前并发数</param>
    public void UpdateConcurrency(string partitionKey, int currentConcurrency)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 更新并发数
        _partitionHealthDict[partitionKey].UpdateConcurrency(currentConcurrency);

        // 评估健康等级（并发数满负荷可能触发警告）
        EvaluateHealthLevel(partitionKey);
    }
    #endregion

    #region 健康状态查询方法（无变量名错误，无需修改）
    /// <summary>
    /// 获取指定分区的健康状态
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <returns>分区健康状态（未初始化则返回null）</returns>
    public PartitionHealthStatus? GetHealthStatus(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        _partitionHealthDict.TryGetValue(partitionKey, out var status);
        return status;
    }

    /// <summary>
    /// 获取所有分区的健康状态
    /// </summary>
    /// <returns>所有分区健康状态列表</returns>
    public List<PartitionHealthStatus> GetAllPartitionStatuses()
    {
        return _partitionHealthDict.Values.ToList();
    }

    /// <summary>
    /// 获取指定健康等级的分区列表
    /// </summary>
    /// <param name="level">健康等级</param>
    /// <returns>符合等级的分区Key列表</returns>
    public List<string> GetPartitionsByHealthLevel(PartitionHealthLevel level)
    {
        return _partitionHealthDict
            .Where(kv => kv.Value.HealthLevel == level)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>
    /// 检查分区是否需要扩容（队列长度超标）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <returns>true=需要扩容，false=不需要</returns>
    public bool NeedExpandPartition(string partitionKey)
    {
        var status = GetHealthStatus(partitionKey);
        return status != null && status.QueueLength >= _options.PartitionExpandThreshold;
    }

    /// <summary>
    /// 检查分区是否需要缩容（队列长度过低）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <returns>true=需要缩容，false=不需要</returns>
    public bool NeedShrinkPartition(string partitionKey)
    {
        var status = GetHealthStatus(partitionKey);
        return status != null && status.QueueLength <= _options.PartitionShrinkThreshold;
    }
    #endregion

    #region 核心监控方法（核心修正：初始化分区时的变量名）
    /// <summary>
    /// 更新失败指标（失败数+失败率）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="queueLength">队列长度</param>
    /// <param name="failureCount">失败数</param>
    /// <param name="failureRate">失败率</param>
    /// <param name="executionMode">执行模式</param>
    /// <param name="currentConcurrency">当前并发数</param>
    public void UpdatePartitionStatus(
        string partitionKey,
        int queueLength,
        double elapsed,
        int failureCount,
        double failureRate,
        ExecutionMode executionMode,
        int currentConcurrency)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 批量更新状态（调用实体的内部方法）
        _partitionHealthDict[partitionKey].UpdatePartitionStatus(
            queueLength,
            failureCount,
            failureRate,
            executionMode,
            currentConcurrency);

        // 同步更新失败时间窗口（如果失败数>0）
        if (failureCount > 0)
        {
            lock (_failureTimeWindowDict[partitionKey])
            {
                // 补充失败时间戳（按失败数添加，简化处理）
                for (int i = 0; i < failureCount; i++)
                {
                    _failureTimeWindowDict[partitionKey].Enqueue(DateTime.UtcNow);
                }
            }
        }

        // 重新评估健康等级
        EvaluateHealthLevel(partitionKey);

        _logger.LogDebug("[TraceId:None] 批量更新分区状态 [PartitionKey:{PartitionKey}, QueueLength:{QueueLength}, ExecutionMode:{ExecutionMode}, CurrentConcurrency:{CurrentConcurrency}]",
            partitionKey, queueLength, executionMode, currentConcurrency);
    }
    #endregion

    #region 核心新增：UpdatePartitionStatus 方法（匹配你的调用参数）
    /// <summary>
    /// 批量更新分区状态（对外暴露，匹配你的调用参数）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="queueLength">队列长度</param>
    /// <param name="failureCount">失败数</param>
    /// <param name="failureRate">失败率</param>
    /// <param name="executionMode">执行模式</param>
    /// <param name="currentConcurrency">当前并发数</param>
    public void UpdatePartitionStatus(
        string partitionKey,
        int queueLength,
        int failureCount,
        double failureRate,
        ExecutionMode executionMode,
        int currentConcurrency)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 确保分区已初始化
        InitializePartition(partitionKey);

        // 批量更新状态（调用实体的内部方法）
        _partitionHealthDict[partitionKey].UpdatePartitionStatus(
            queueLength,
            failureCount,
            failureRate,
            executionMode,
            currentConcurrency);

        // 同步更新失败时间窗口（如果失败数>0）
        if (failureCount > 0)
        {
            lock (_failureTimeWindowDict[partitionKey])
            {
                // 补充失败时间戳（按失败数添加，简化处理）
                for (int i = 0; i < failureCount; i++)
                {
                    _failureTimeWindowDict[partitionKey].Enqueue(DateTime.UtcNow);
                }
            }
        }

        // 重新评估健康等级
        EvaluateHealthLevel(partitionKey);

        _logger.LogDebug("[TraceId:None] 批量更新分区状态 [PartitionKey:{PartitionKey}, QueueLength:{QueueLength}, ExecutionMode:{ExecutionMode}, CurrentConcurrency:{CurrentConcurrency}]",
            partitionKey, queueLength, executionMode, currentConcurrency);
    }
    #endregion

    #region 核心新增：UpdatePartitionCount 方法
    /// <summary>
    /// 更新全局分区总数（对外暴露，接收新的分区数）
    /// 【设计思路】：同步全局分区数量，便于健康检查器评估整体负载
    /// </summary>
    /// <param name="newCount">新的分区总数</param>
    public void UpdatePartitionCount(int newCount)
    {
        if (newCount < _options.MinPartitionCount || newCount > _options.MaxPartitionCount)
        {
            _logger.LogWarning("[TraceId:None] 分区数更新失败：新值{NewCount}超出阈值（最小{Min}, 最大{Max}）",
                newCount, _options.MinPartitionCount, _options.MaxPartitionCount);
            return;
        }

        var oldCount = _currentPartitionCount;
        _currentPartitionCount = newCount;

        _logger.LogInformation("[TraceId:None] 全局分区总数已更新 [旧值:{OldCount}, 新值:{NewCount}]",
            oldCount, newCount);
    }

    /// <summary>
    /// 获取当前全局分区总数
    /// </summary>
    /// <returns>当前分区总数</returns>
    public int GetCurrentPartitionCount()
    {
        return _currentPartitionCount;
    }
    #endregion

    #region 内部逻辑
    private void UpdateFailureMetrics(string partitionKey)
    {
        var failureQueue = _failureTimeWindowDict[partitionKey];
        var now = DateTime.UtcNow;

        // 统计1分钟内的失败数
        int failureCount;
        lock (failureQueue)
        {
            failureCount = failureQueue.Count(t => t >= now.AddMinutes(-1));
        }

        // 计算失败率
        double failureRate = failureCount / Math.Max(1.0, _options.BatchSize * 60 / _options.BatchInterval.TotalSeconds);

        // 更新到健康状态
        _partitionHealthDict[partitionKey].UpdateFailure(failureCount, failureRate);
    }

    /// <summary>
    /// 评估分区健康等级（核心判定逻辑）
    /// </summary>
    private void EvaluateHealthLevel(string partitionKey)
    {
        var status = _partitionHealthDict[partitionKey];
        var isCircuitBroken = _circuitBreakerStrategy.IsOpen(partitionKey);

        // 1. 优先判断熔断状态
        if (isCircuitBroken)
        {
            status.UpdateCircuitStatus(true);
            status.UpdateHealthLevel(PartitionHealthLevel.CircuitBroken);
            return;
        }

        // 2. 重置熔断状态（已恢复）
        status.UpdateCircuitStatus(false);

        // 3. 判断异常等级（失败率/队列长度超标）
        bool isAbnormal = status.FailureRateInMinute >= _abnormalFailureRateThreshold ||
                          status.QueueLength >= _abnormalQueueLengthThreshold ||
                          status.CurrentConcurrency >= status.MaxConcurrency;

        // 4. 判断警告等级（接近阈值）
        bool isWarning = !isAbnormal &&
                         (status.FailureRateInMinute >= _warningFailureRateThreshold ||
                          status.QueueLength >= _warningQueueLengthThreshold);

        // 5. 更新健康等级
        var newLevel = isAbnormal ? PartitionHealthLevel.Abnormal :
                       isWarning ? PartitionHealthLevel.Warning :
                       PartitionHealthLevel.Healthy;

        // 等级变化时记录日志
        if (status.HealthLevel != newLevel)
        {
            _logger.LogInformation("[TraceId:None] 分区健康等级变化 [PartitionKey:{PartitionKey}, 旧等级:{OldLevel}, 新等级:{NewLevel}, 失败率:{FailureRate}%, 队列长度:{QueueLength}]",
                partitionKey, status.HealthLevel, newLevel, Math.Round(status.FailureRateInMinute * 100, 2), status.QueueLength);
        }

        status.UpdateHealthLevel(newLevel);
    }

    /// <summary>
    /// 清理过期失败记录（每分钟执行）
    /// </summary>
    private void CleanupExpiredFailures(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-1);
            int totalCleaned = 0;

            foreach (var kv in _failureTimeWindowDict)
            {
                lock (kv.Value)
                {
                    // 移除1分钟前的失败记录
                    while (kv.Value.Count > 0 && kv.Value.Peek() < cutoffTime)
                    {
                        kv.Value.Dequeue();
                        totalCleaned++;
                    }
                }

                // 清理后更新失败指标
                UpdateFailureMetrics(kv.Key);
                EvaluateHealthLevel(kv.Key);
            }

            if (totalCleaned > 0)
            {
                _logger.LogDebug("[TraceId:None] 清理过期失败记录完成，共清理{TotalCleaned}条", totalCleaned);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 清理过期失败记录异常");
        }
    }
    #endregion

    #region 资源释放（无变量名错误）
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer.Dispose();
        _partitionHealthDict.Clear();
        _failureTimeWindowDict.Clear();
        _logger.LogInformation("分区健康检查器已释放资源");
    }
    #endregion
}
