using Artizan.IoT.BatchProcessing.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 兜底存储接口
/// 【设计思路】：失败消息的最终存储，保证数据不丢失
/// 【设计考量】：
/// 1. 支持单条/批量存储，适配不同失败场景
/// 2. 记录失败原因和类型，便于后续补偿和问题排查
/// 3. 提供读取/删除接口，支持补偿处理
/// 【设计模式】：仓库模式（Repository Pattern）
/// </summary>
/// <typeparam name="TMessage">消息类型</typeparam>
public interface IBatchFallbackStore<TMessage>
{
    /// <summary>
    /// 存储单条兜底消息
    /// </summary>
    /// <param name="message">待存储消息</param>
    /// <param name="reason">失败原因</param>
    /// <param name="fallbackType">兜底类型</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="messageId">消息唯一标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储结果</returns>
    Task StoreAsync(
        TMessage message,
        string reason,
        FallbackType fallbackType,
        string traceId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量存储兜底消息
    /// </summary>
    /// <param name="messages">批量消息列表</param>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="fallbackType">兜底类型</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储结果</returns>
    Task StoreBatchAsync(
        List<TMessage> messages,
        string partitionKey,
        FallbackType fallbackType,
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取指定分区的兜底消息（用于补偿）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>兜底消息列表</returns>
    Task<List<TMessage>> ReadAsync(
        string partitionKey,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除已处理的兜底消息
    /// </summary>
    /// <param name="messageIds">消息ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    Task DeleteAsync(
        List<string> messageIds,
        CancellationToken cancellationToken = default);
}