using Artizan.IoT.BatchProcessing.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 固定间隔重试策略
/// 【设计思路】：失败后按固定间隔重试，达到最大次数后抛出异常
/// 【设计考量】：
/// 1. 支持自定义最大重试次数和重试间隔
/// 2. 完整的重试日志，便于问题排查
/// 3. 取消令牌支持，可随时终止重试
/// 【设计模式】：策略模式
/// </summary>
public class FixedIntervalRetryStrategy : IRetryStrategy
{
    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<FixedIntervalRetryStrategy> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志器</param>
    public FixedIntervalRetryStrategy(ILogger<FixedIntervalRetryStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行带重试的异步操作
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="operation">待执行操作</param>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="retryInterval">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <exception cref="AggregateException">所有重试失败后抛出</exception>
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        string partitionKey,
        int maxRetryCount,
        TimeSpan retryInterval,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        if (maxRetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "最大重试次数不能小于0");
        }

        var exceptions = new List<Exception>();
        var attempt = 0;

        while (attempt <= maxRetryCount)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug(
                    "[TraceId:None] 执行重试操作 [PartitionKey:{PartitionKey}, Attempt:{Attempt}, MaxRetries:{MaxRetries}]",
                    partitionKey,
                    attempt,
                    maxRetryCount);

                // 执行目标操作
                return await operation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[TraceId:None] 重试操作被取消 [PartitionKey:{PartitionKey}, Attempt:{Attempt}]",
                    partitionKey,
                    attempt);
                throw;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                attempt++;

                // 达到最大重试次数，抛出异常
                if (attempt > maxRetryCount)
                {
                    _logger.LogError(
                        ex,
                        "[TraceId:None] 重试操作失败，已达最大重试次数 [PartitionKey:{PartitionKey}, Attempt:{Attempt}, MaxRetries:{MaxRetries}]",
                        partitionKey,
                        attempt - 1,
                        maxRetryCount);

                    throw new AggregateException($"重试{maxRetryCount}次后仍失败", exceptions);
                }

                _logger.LogWarning(
                    ex,
                    "[TraceId:None] 重试操作失败，将重试 [PartitionKey:{PartitionKey}, Attempt:{Attempt}, MaxRetries:{MaxRetries}, RetryInterval:{RetryInterval}ms]",
                    partitionKey,
                    attempt - 1,
                    maxRetryCount,
                    retryInterval.TotalMilliseconds);

                // 等待重试间隔
                await Task.Delay(retryInterval, cancellationToken);
            }
        }

        // 理论上不会走到这里
        throw new AggregateException($"重试{maxRetryCount}次后仍失败", exceptions);
    }
}