using Artizan.IoT.BatchProcessing.Enums;
using System;

namespace Artizan.IoT.BatchProcessing.Health;

/// <summary>
/// 分区健康状态实体（存储单分区的所有健康指标）
/// 【设计思路】：
/// 1. 覆盖核心监控维度：并发、失败、队列、时间、策略状态
/// 2. 所有指标为只读（通过健康检查器更新），保证线程安全
/// 3. 包含时间戳，便于追踪状态变化
/// </summary>
public class PartitionHealthStatus
{
    /// <summary>
    /// 分区唯一标识
    /// </summary>
    public string PartitionKey { get; init; } = string.Empty;

    /// <summary>
    /// 健康等级（核心判定结果）
    /// </summary>
    public PartitionHealthLevel HealthLevel { get; private set; } = PartitionHealthLevel.Healthy;

    /// <summary>
    /// 当前并发数（隔离策略）
    /// </summary>
    public int CurrentConcurrency { get; private set; } = 0;

    /// <summary>
    /// 最大并发数（隔离策略）
    /// </summary>
    public int MaxConcurrency { get; init; } = 5;

    /// <summary>
    /// 失败次数（最近1分钟）
    /// </summary>
    public int FailureCountInMinute { get; private set; } = 0;

    /// <summary>
    /// 失败率（最近1分钟，0~1）
    /// </summary>
    public double FailureRateInMinute { get; private set; } = 0;

    /// <summary>
    /// 队列长度（待处理消息数）
    /// </summary>
    public int QueueLength { get; private set; } = 0;

    /// <summary>
    /// 最后一次处理成功时间（UTC）
    /// </summary>
    public DateTime LastSuccessTime { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// 最后一次处理失败时间（UTC）
    /// </summary>
    public DateTime LastFailureTime { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// 是否熔断（熔断策略状态）
    /// </summary>
    public bool IsCircuitBroken { get; private set; } = false;

    /// <summary>
    /// 状态最后更新时间（UTC）
    /// </summary>
    public DateTime LastUpdateTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 分区当前执行模式（新增：适配 UpdatePartitionStatus 传入的参数）
    /// </summary>
    public ExecutionMode ExecutionMode { get; private set; } = ExecutionMode.Parallel;

    /// <summary>
    /// 健康等级描述（便于监控展示）
    /// </summary>
    public string HealthDescription => HealthLevel switch
    {
        PartitionHealthLevel.Healthy => "分区健康，所有指标正常",
        PartitionHealthLevel.Warning => "分区警告：队列长度/失败率接近阈值",
        PartitionHealthLevel.Abnormal => "分区异常：失败率/并发数超标，处理效率下降",
        PartitionHealthLevel.CircuitBroken => "分区熔断：已停止处理，需等待恢复",
        _ => "未知状态"
    };

    #region 内部更新方法（仅健康检查器可调用）
    /// <summary>
    /// 重置对象状态（归还到池时调用）
    /// </summary>
    public void Reset()
    {
        CurrentConcurrency = 0;
        FailureCountInMinute = 0;
        FailureRateInMinute = 0;
        QueueLength = 0;
        LastSuccessTime = DateTime.MinValue;
        LastFailureTime = DateTime.MinValue;
        IsCircuitBroken = false;
        ExecutionMode = ExecutionMode.Parallel;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新并发数
    /// </summary>
    internal void UpdateConcurrency(int currentConcurrency)
    {
        CurrentConcurrency = currentConcurrency;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新失败指标
    /// </summary>
    internal void UpdateFailure(int failureCount, double failureRate)
    {
        FailureCountInMinute = failureCount;
        FailureRateInMinute = failureRate;
        LastFailureTime = DateTime.UtcNow;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新队列长度
    /// </summary>
    internal void UpdateQueueLength(int queueLength)
    {
        QueueLength = queueLength;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新处理成功时间
    /// </summary>
    internal void UpdateSuccessTime()
    {
        LastSuccessTime = DateTime.UtcNow;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新熔断状态
    /// </summary>
    internal void UpdateCircuitStatus(bool isBroken)
    {
        IsCircuitBroken = isBroken;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新健康等级（核心判定）
    /// </summary>
    internal void UpdateHealthLevel(PartitionHealthLevel level)
    {
        HealthLevel = level;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新执行模式
    /// </summary>
    internal void UpdateExecutionMode(ExecutionMode mode)
    {
        ExecutionMode = mode;
        LastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 批量更新分区状态（核心）
    /// </summary>
    /// <param name="queueLength">队列长度</param>
    /// <param name="failureCount">失败数</param>
    /// <param name="failureRate">失败率</param>
    /// <param name="executionMode">执行模式</param>
    /// <param name="currentConcurrency">当前并发数</param>
    internal void UpdatePartitionStatus(
        int queueLength,
        int failureCount,
        double failureRate,
        ExecutionMode executionMode,
        int currentConcurrency)
    {
        QueueLength = queueLength;
        FailureCountInMinute = failureCount;
        FailureRateInMinute = failureRate;
        ExecutionMode = executionMode;
        CurrentConcurrency = currentConcurrency;
        LastUpdateTime = DateTime.UtcNow;
    }
    #endregion
}