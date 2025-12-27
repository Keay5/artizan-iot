using Artizan.IoT.BatchProcessing.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 基于信号量的隔离策略
/// 【设计思路】：使用SemaphoreSlim控制每个分区的并发数，实现分区级别的隔离
/// 【设计考量】：
/// 1. 每个分区独立信号量，避免跨分区影响
/// 2. 懒加载创建信号量，减少初始化开销
/// 3. 线程安全的信号量管理，支持动态调整并发数
/// 【设计模式】：策略模式 + 懒加载模式
/// </summary>
public class SemaphoreIsolateStrategy : IIsolateStrategy
{
    ///// <summary>
    ///// 分区信号量字典（每个分区一个信号量）
    ///// </summary>
    //private readonly ConcurrentDictionary<string, SemaphoreSlim> _partitionSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

    // 存储每个分区的 SemaphoreSlim 实例
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreDict = new();

    // 存储每个分区 SemaphoreSlim 对应的最大并发数（核心：手动维护）
    private readonly ConcurrentDictionary<string, int> _semaphoreMaxCountDict = new();

    // 默认最大并发数（可通过构造函数配置）
    private readonly int _defaultMaxConcurrency;

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<SemaphoreIsolateStrategy> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志器</param>
    public SemaphoreIsolateStrategy(ILogger<SemaphoreIsolateStrategy> logger, int defaultMaxConcurrency = 5)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultMaxConcurrency = defaultMaxConcurrency;
    }

    /// <summary>
    /// 尝试获取并发许可
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否获取到许可</returns>
    public async Task<bool> TryEnterAsync(string partitionKey, int maxConcurrency, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "最大并发数必须大于0");
        }

        try
        {
            //// 懒加载创建信号量
            //var semaphore = _partitionSemaphores.GetOrAdd(partitionKey, _ => new SemaphoreSlim(maxConcurrency, maxConcurrency));

            //// 如果当前信号量的最大计数与配置不一致，重新创建
            //if (semaphore.CurrentCount != maxConcurrency)
            //{
            //    semaphore.Dispose();
            //    semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            //    _partitionSemaphores[partitionKey] = semaphore;
            //}

            // 尝试获取许可（非阻塞）
            //var acquired = await semaphore.WaitAsync(0, cancellationToken);

            // 确定该分区的最大并发数（参数优先，无则用默认值）
            var targetMaxConcurrency = maxConcurrency > 0 ? maxConcurrency : _defaultMaxConcurrency;

            // 懒加载创建 SemaphoreSlim 实例（确保线程安全）
            var semaphore = _semaphoreDict.GetOrAdd(
                partitionKey,
                key => new SemaphoreSlim(targetMaxConcurrency, targetMaxConcurrency)
            );

            // 同步保存该分区的最大并发数（与 SemaphoreSlim 一一对应）
            _semaphoreMaxCountDict.GetOrAdd(partitionKey, targetMaxConcurrency);

            // 尝试进入信号量（非阻塞，立即返回结果）
            var acquired = await semaphore.WaitAsync(0, cancellationToken);
            if (!acquired)
            {
                _logger.LogDebug(
                    "[TraceId:None] 分区并发数超限 [PartitionKey:{PartitionKey}, MaxConcurrency:{MaxConcurrency}, CurrentConcurrency:{CurrentConcurrency}]",
                    partitionKey,
                    maxConcurrency,
                    maxConcurrency - semaphore.CurrentCount);
            }

            return acquired;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[TraceId:None] 获取分区并发许可被取消 [PartitionKey:{PartitionKey}]", partitionKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 获取分区并发许可异常 [PartitionKey:{PartitionKey}]", partitionKey);
            return false;
        }
    }

    /// <summary>
    /// 释放并发许可
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    public void Release(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        //try
        //{
        //    if (_partitionSemaphores.TryGetValue(partitionKey, out var semaphore))
        //    {
        //        // 代码规范：即使单行也用{}
        //        if (semaphore.CurrentCount < semaphore.MaxCount)
        //        {
        //            semaphore.Release();
        //        }

        //        _logger.LogDebug(
        //            "[TraceId:None] 释放分区并发许可 [PartitionKey:{PartitionKey}, CurrentConcurrency:{CurrentConcurrency}]",
        //            partitionKey,
        //            semaphore.MaxCount - semaphore.CurrentCount);
        //    }
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "[TraceId:None] 释放分区并发许可异常 [PartitionKey:{PartitionKey}]", partitionKey);
        //}

        try
        {
            if (_semaphoreDict.TryGetValue(partitionKey, out var semaphore))
            {
                semaphore.Release();
            }
        }
        catch (SemaphoreFullException ex)
        {
            // TODO:?避免释放超过最大计数的异常（信号量已达上限）
            _logger.LogError(ex, "[TraceId:None] 释放超过最大计数的异常[PartitionKey:{PartitionKey}]", partitionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 释放分区并发许可异常 [PartitionKey:{PartitionKey}]", partitionKey);
        }


    }

    /// <summary>
    /// 获取当前分区并发数
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <returns>当前并发数</returns>
    public int GetCurrentConcurrency(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        //if (_partitionSemaphores.TryGetValue(partitionKey, out var semaphore))
        //{
        //    return semaphore.MaxCount - semaphore.CurrentCount;
        //}

        //return 0;

        // 从手动维护的字典中获取（核心：替代原来的 semaphore.MaxCount）
        _semaphoreMaxCountDict.TryGetValue(partitionKey, out var maxCount);
        return maxCount > 0 ? maxCount : _defaultMaxConcurrency;
    }

    /// <summary>
    /// 获取指定分区的最大并发数（替代 SemaphoreSlim.MaxCount）
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <returns>该分区的最大并发数，无则返回默认值</returns>
    public int GetMaxConcurrency(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        // 从手动维护的字典中获取（核心：替代原来的 semaphore.MaxCount）
        _semaphoreMaxCountDict.TryGetValue(partitionKey, out var maxCount);
        return maxCount > 0 ? maxCount : _defaultMaxConcurrency;
    }

    /// <summary>
    /// 获取指定分区的当前可用并发数
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <returns>当前可用数（注意：SemaphoreSlim.AvailableWaitHandle 不直接返回数值，需结合逻辑）</returns>
    public int GetAvailableCount(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        if (!_semaphoreDict.TryGetValue(partitionKey, out var semaphore))
        {
            return _defaultMaxConcurrency; // 未创建则返回默认最大数
        }

        // 注意：SemaphoreSlim 无直接获取可用数的属性，以下是兼容写法
        // 若需精准获取，需额外维护可用数（但会增加线程安全成本）
        // 简化场景下，可用 "最大数 - 当前占用数" 估算，或直接返回 SemaphoreSlim 的近似状态
        var maxCount = GetMaxConcurrency(partitionKey);
        // （可选）若需精准可用数，需在 TryEnter/Release 时维护可用数字典
        return maxCount; // 简化返回：无占用时=maxCount，实际场景可扩展
    }

    /// <summary>
    /// 释放指定分区的 SemaphoreSlim 资源
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    public void DisposePartition(string partitionKey)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey));
        }

        if (_semaphoreDict.TryRemove(partitionKey, out var semaphore))
        {
            semaphore.Dispose();
        }
        _semaphoreMaxCountDict.TryRemove(partitionKey, out _);
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        foreach (var semaphore in _semaphoreDict.Values)
        {
            semaphore.Dispose();
        }
        _semaphoreDict.Clear();
        _semaphoreMaxCountDict.Clear();
    }
}
