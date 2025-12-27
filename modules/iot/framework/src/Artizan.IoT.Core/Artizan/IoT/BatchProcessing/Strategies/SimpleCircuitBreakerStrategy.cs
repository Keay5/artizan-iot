using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 简单熔断器策略（基于失败次数）
/// 【设计思路】：实现经典的熔断器模式（关闭→打开→半开）
/// 【设计考量】：
/// 1. 每个分区独立熔断器状态，避免跨分区影响
/// 2. 失败次数达阈值时打开熔断器，快速失败
/// 3. 熔断器打开后，等待恢复时间自动进入半开状态
/// 4. 半开状态下成功一次即关闭熔断器
/// 【设计模式】：策略模式 + 状态模式
/// </summary>
public class SimpleCircuitBreakerStrategy : ICircuitBreakerStrategy
{
    /// <summary>
    /// 熔断器状态枚举
    /// </summary>
    private enum CircuitState
    {
        Closed,   // 关闭（正常处理）
        Open,     // 打开（快速失败）
        HalfOpen  // 半开（尝试恢复）
    }

    /// <summary>
    /// 分区熔断器状态
    /// </summary>
    private class PartitionCircuitState
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public CircuitState State { get; set; } = CircuitState.Closed;

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// 熔断器打开时间
        /// </summary>
        public DateTime OpenTime { get; set; } = DateTime.MinValue;
    }

    /// <summary>
    /// 分区熔断器状态字典
    /// </summary>
    private readonly ConcurrentDictionary<string, PartitionCircuitState> _partitionStates = new ConcurrentDictionary<string, PartitionCircuitState>();

    /// <summary>
    /// 批处理配置
    /// </summary>
    private readonly BatchProcessingOptions _options;

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<SimpleCircuitBreakerStrategy> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">批处理配置</param>
    /// <param name="logger">日志器</param>
    public SimpleCircuitBreakerStrategy(
        IOptions<BatchProcessingOptions> options,
        ILogger<SimpleCircuitBreakerStrategy> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 判断熔断器是否打开
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <returns>是否打开</returns>
    public bool IsOpen(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        var state = _partitionStates.GetOrAdd(partitionKey, _ => new PartitionCircuitState());
        var now = DateTime.UtcNow;

        // 线程安全的状态检查
        lock (state)
        {
            // 如果熔断器打开且超过恢复时间，进入半开状态
            if (state.State == CircuitState.Open && (now - state.OpenTime) >= _options.CircuitBreakerRecoveryTime)
            {
                state.State = CircuitState.HalfOpen;
                _logger.LogWarning(
                    "[TraceId:None] 分区熔断器进入半开状态 [PartitionKey:{PartitionKey}, OpenTime:{OpenTime}, RecoveryTime:{RecoveryTime}s]",
                    partitionKey,
                    state.OpenTime,
                    _options.CircuitBreakerRecoveryTime.TotalSeconds);
            }

            // 只有关闭状态才正常处理
            return state.State != CircuitState.Closed;
        }
    }

    /// <summary>
    /// 记录失败
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    public void RecordFailure(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        var state = _partitionStates.GetOrAdd(partitionKey, _ => new PartitionCircuitState());

        lock (state)
        {
            // 半开状态下失败，直接打开熔断器
            if (state.State == CircuitState.HalfOpen)
            {
                state.State = CircuitState.Open;
                state.OpenTime = DateTime.UtcNow;
                state.FailureCount = 0;
                _logger.LogError(
                    "[TraceId:None] 分区熔断器半开状态失败，重新打开 [PartitionKey:{PartitionKey}]",
                    partitionKey);
                return;
            }

            // 关闭状态下累计失败次数
            state.FailureCount++;
            _logger.LogDebug(
                "[TraceId:None] 分区处理失败，累计失败次数 [PartitionKey:{PartitionKey}, FailureCount:{FailureCount}, Threshold:{Threshold}]",
                partitionKey,
                state.FailureCount,
                _options.CircuitBreakerFailureThreshold);

            // 失败次数达阈值，打开熔断器
            if (state.FailureCount >= _options.CircuitBreakerFailureThreshold)
            {
                state.State = CircuitState.Open;
                state.OpenTime = DateTime.UtcNow;
                _logger.LogError(
                    "[TraceId:None] 分区熔断器打开 [PartitionKey:{PartitionKey}, FailureCount:{FailureCount}, RecoveryTime:{RecoveryTime}s]",
                    partitionKey,
                    state.FailureCount,
                    _options.CircuitBreakerRecoveryTime.TotalSeconds);
            }
        }
    }

    /// <summary>
    /// 记录成功
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    public void RecordSuccess(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        var state = _partitionStates.GetOrAdd(partitionKey, _ => new PartitionCircuitState());

        lock (state)
        {
            // 半开状态下成功，关闭熔断器
            if (state.State == CircuitState.HalfOpen)
            {
                state.State = CircuitState.Closed;
                state.FailureCount = 0;
                _logger.LogInformation(
                    "[TraceId:None] 分区熔断器半开状态成功，关闭熔断器 [PartitionKey:{PartitionKey}]",
                    partitionKey);
                return;
            }

            // 关闭状态下重置失败次数
            if (state.State == CircuitState.Closed && state.FailureCount > 0)
            {
                state.FailureCount = 0;
                _logger.LogDebug("[TraceId:None] 分区处理成功，重置失败次数 [PartitionKey:{PartitionKey}]", partitionKey);
            }
        }
    }
}