using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 执行顺序策略（串行/并行控制）
/// 【设计思路】：控制每个分区的消息处理顺序，支持动态切换
/// 【设计考量】：
/// 1. 串行：使用锁保证单线程处理，保证顺序
/// 2. 并行：直接执行，多线程处理
/// 3. 模式切换时使用超时控制，避免死锁
/// 【设计模式】：策略模式 + 状态模式
/// </summary>
public class ExecutionOrderStrategy : IExecutionOrderStrategy
{
    /// <summary>
    /// 分区执行模式字典
    /// </summary>
    private readonly ConcurrentDictionary<string, ExecutionMode> _partitionModes = new ConcurrentDictionary<string, ExecutionMode>();

    /// <summary>
    /// 分区锁字典（用于串行执行）
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _partitionLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<ExecutionOrderStrategy> _logger;

    /// <summary>
    /// 默认执行模式
    /// </summary>
    private readonly ExecutionMode _defaultMode;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="defaultMode">默认执行模式</param>
    public ExecutionOrderStrategy(ILogger<ExecutionOrderStrategy> logger, ExecutionMode defaultMode = ExecutionMode.Parallel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultMode = defaultMode;
    }

    /// <summary>
    /// 按指定模式执行操作
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="operation">待执行操作</param>
    /// <param name="mode">执行模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    public async Task<TResult> ExecuteAsync<TResult>(
        string partitionKey,
        Func<CancellationToken, Task<TResult>> operation,
        ExecutionMode mode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        // 更新当前分区模式
        _partitionModes[partitionKey] = mode;

        try
        {
            // 串行模式：使用信号量保证单线程执行
            if (mode == ExecutionMode.Serial)
            {
                var semaphore = _partitionLocks.GetOrAdd(partitionKey, _ => new SemaphoreSlim(1, 1));

                // 等待获取锁（支持取消）
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    _logger.LogDebug("[TraceId:None] 串行执行分区操作 [PartitionKey:{PartitionKey}]", partitionKey);
                    return await operation(cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            // 并行模式：直接执行
            else
            {
                _logger.LogDebug("[TraceId:None] 并行执行分区操作 [PartitionKey:{PartitionKey}]", partitionKey);
                return await operation(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[TraceId:None] 执行分区操作被取消 [PartitionKey:{PartitionKey}, Mode:{Mode}]", partitionKey, mode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 执行分区操作异常 [PartitionKey:{PartitionKey}, Mode:{Mode}]", partitionKey, mode);
            throw;
        }
    }

    /// <summary>
    /// 切换分区执行模式
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="newMode">新模式</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否切换成功</returns>
    public async Task<bool> ChangeExecutionModeAsync(
        string partitionKey,
        ExecutionMode newMode,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        _logger.LogDebug("[TraceId:None] 尝试切换分区执行模式 [PartitionKey:{PartitionKey}, NewMode:{NewMode}, Timeout:{Timeout}s]",
            partitionKey, newMode, timeout.TotalSeconds);

        // 如果是切换到串行模式，需要等待当前并行操作完成
        if (newMode == ExecutionMode.Serial)
        {
            var semaphore = _partitionLocks.GetOrAdd(partitionKey, _ => new SemaphoreSlim(1, 1));

            try
            {
                // 尝试获取锁（带超时）
                var acquired = await semaphore.WaitAsync(timeout, cancellationToken);

                if (acquired)
                {
                    try
                    {
                        // 成功获取锁，更新模式
                        _partitionModes[partitionKey] = newMode;
                        return true;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[TraceId:None] 切换分区执行模式被取消 [PartitionKey:{PartitionKey}, NewMode:{NewMode}]", partitionKey, newMode);
                return false;
            }
        }
        // 切换到并行模式：直接更新
        else
        {
            _partitionModes[partitionKey] = newMode;
            return true;
        }
    }
}
