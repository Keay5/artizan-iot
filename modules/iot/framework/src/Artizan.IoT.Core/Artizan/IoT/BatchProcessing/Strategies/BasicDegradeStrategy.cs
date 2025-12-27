using Artizan.IoT.BatchProcessing.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Strategies;

/// <summary>
/// 基础降级策略
/// 【设计思路】：降级时仅记录日志，不执行核心业务逻辑
/// 【设计考量】：
/// 1. 简单降级策略，适合大多数场景
/// 2. 完整的降级日志，便于后续补偿
/// 3. 无副作用，仅记录不处理
/// 【设计模式】：策略模式
/// </summary>
public class BasicDegradeStrategy : IDegradeStrategy
{
    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<BasicDegradeStrategy> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志器</param>
    public BasicDegradeStrategy(ILogger<BasicDegradeStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行降级逻辑
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="messages">消息列表</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public Task ExecuteAsync(string partitionKey, List<object> messages, string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            throw new ArgumentException("分区Key不能为空", nameof(partitionKey));
        }

        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (string.IsNullOrEmpty(traceId))
        {
            throw new ArgumentException("追踪ID不能为空", nameof(traceId));
        }

        // 代码规范：即使单行也用{}
        if (messages.Count == 0)
        {
            _logger.LogDebug("[TraceId:{TraceId}] 无消息需要降级处理 [PartitionKey:{PartitionKey}]", traceId, partitionKey);
            return Task.CompletedTask;
        }

        _logger.LogWarning(
            "[TraceId:{TraceId}] 执行降级策略 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}, MessageType:{MessageType}]",
            traceId,
            partitionKey,
            messages.Count,
            messages[0]?.GetType().FullName ?? "Unknown");

        // 基础降级：仅记录日志，可在子类扩展（如存储到降级队列、发送告警等）
        return Task.CompletedTask;
    }
}