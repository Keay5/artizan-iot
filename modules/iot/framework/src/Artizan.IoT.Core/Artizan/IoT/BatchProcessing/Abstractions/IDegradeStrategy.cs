using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 降级策略接口
/// 【设计思路】：服务降级，保障核心功能可用
/// 【设计考量】：
/// 1. 熔断/隔离触发时执行降级逻辑，避免服务不可用
/// 2. 支持批量消息降级处理，提升效率
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IDegradeStrategy
{
    /// <summary>
    /// 执行降级逻辑
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="messages">待降级处理的消息列表</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>降级执行结果</returns>
    Task ExecuteAsync(
        string partitionKey,
        List<object> messages,
        string traceId,
        CancellationToken cancellationToken = default);
}