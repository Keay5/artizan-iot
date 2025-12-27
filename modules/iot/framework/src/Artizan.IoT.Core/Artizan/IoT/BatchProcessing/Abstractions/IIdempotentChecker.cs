using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 幂等性校验接口
/// 【设计思路】：防止消息重复处理，保证数据一致性
/// 【设计考量】：
/// 1. 基于消息ID校验，确保每条消息仅处理一次
/// 2. 支持过期清理，避免存储膨胀
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IIdempotentChecker
{
    /// <summary>
    /// 检查消息是否已处理
    /// </summary>
    /// <param name="messageId">消息唯一ID</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true=已处理，false=未处理</returns>
    Task<bool> CheckAsync(string messageId, string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记消息为已处理
    /// </summary>
    /// <param name="messageId">消息唯一ID</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标记结果</returns>
    Task MarkAsProcessedAsync(string messageId, string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的幂等记录
    /// </summary>
    /// <param name="expiration">过期时间</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数</returns>
    Task<long> CleanupExpiredAsync(TimeSpan expiration, string traceId, CancellationToken cancellationToken = default);
}