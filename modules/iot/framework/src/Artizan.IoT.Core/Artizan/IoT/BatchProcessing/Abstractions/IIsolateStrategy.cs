using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 隔离策略接口
/// 【设计思路】：控制单个分区的并发处理数，防止单分区过载影响整体服务
/// 【设计考量】：
/// 1. 支持并发数限制，避免资源竞争导致的性能下降
/// 2. 提供释放和查询接口，便于监控分区并发状态
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IIsolateStrategy : IDisposable
{
    /// <summary>
    /// 尝试获取并发许可（进入隔离）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否获取到许可</returns>
    Task<bool> TryEnterAsync(string partitionKey, int maxConcurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// 释放并发许可（退出隔离）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    void Release(string partitionKey);

    /// <summary>
    /// 获取当前分区的并发数
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <returns>当前并发数</returns>
    int GetCurrentConcurrency(string partitionKey);
}