using Artizan.IoT.BatchProcessing.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 哈希分区策略
/// 【设计思路】：基于消息的哈希值进行分区，保证相同消息分到同一分区
/// 【设计考量】：
/// 1. 支持任意类型消息，优先使用IHasId接口的ID
/// 2. 哈希值取模，均匀分布到各个分区
/// 3. 线程安全，无状态设计
/// 【设计模式】：策略模式
/// </summary>
public class HashPartitionStrategy : IUpdatablePartitionStrategy
{
    /// <summary>
    /// 当前分区数
    /// </summary>
    private int _currentPartitionCount = 8;

    /// <summary>
    /// 获取分区Key
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="partitionCount">分区数</param>
    /// <returns>分区Key</returns>
    public string GetPartitionKey(object message, int partitionCount)
    {
        if (partitionCount <= 0)
        {
            partitionCount = _currentPartitionCount;
        }

        if (partitionCount <= 0)
        {
            partitionCount = 8;
        }

        // 计算哈希值
        int hash = 0;

        // 如果消息实现了IHasId接口，使用ID计算哈希
        if (message is IHasId hasIdMessage)
        {
            hash = hasIdMessage.Id.GetHashCode();
        }
        // 否则使用消息本身的哈希值
        else if (message != null)
        {
            hash = message.GetHashCode();
        }

        // 取模计算分区索引（保证非负）
        var partitionIndex = Math.Abs(hash) % partitionCount;

        return $"partition_{partitionIndex}";
    }

    /// <summary>
    /// 更新分区数
    /// </summary>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public Task UpdatePartitionCountAsync(int newCount, string traceId, CancellationToken cancellationToken)
    {
        if (newCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCount), "分区数必须大于0");
        }

        Interlocked.Exchange(ref _currentPartitionCount, newCount);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 范围分区策略（仅支持IHasId接口的消息）
/// 【设计思路】：基于消息ID的范围进行分区
/// 【设计考量】：
/// 1. 仅支持IHasId接口的消息
/// 2. 按ID范围均匀分区，适合有序ID场景
/// 【设计模式】：策略模式
/// </summary>
public class RangePartitionStrategy : IUpdatablePartitionStrategy
{
    /// <summary>
    /// 当前分区数
    /// </summary>
    private int _currentPartitionCount = 8;

    /// <summary>
    /// 每个分区的ID范围
    /// </summary>
    private long _rangePerPartition = long.MaxValue / 8;

    /// <summary>
    /// 获取分区Key
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="partitionCount">分区数</param>
    /// <returns>分区Key</returns>
    public string GetPartitionKey(object message, int partitionCount)
    {
        if (partitionCount <= 0)
        {
            partitionCount = _currentPartitionCount;
        }

        if (partitionCount <= 0)
        {
            partitionCount = 8;
        }

        // 仅支持IHasId接口的消息
        if (!(message is IHasId hasIdMessage))
        {
            throw new NotSupportedException("范围分区策略仅支持实现IHasId接口的消息");
        }

        // 计算分区索引
        var partitionIndex = (int)(hasIdMessage.Id / _rangePerPartition) % partitionCount;
        partitionIndex = Math.Abs(partitionIndex);

        return $"partition_{partitionIndex}";
    }

    /// <summary>
    /// 更新分区数
    /// </summary>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public Task UpdatePartitionCountAsync(int newCount, string traceId, CancellationToken cancellationToken)
    {
        if (newCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCount), "分区数必须大于0");
        }

        Interlocked.Exchange(ref _currentPartitionCount, newCount);
        Interlocked.Exchange(ref _rangePerPartition, long.MaxValue / newCount);

        return Task.CompletedTask;
    }
}