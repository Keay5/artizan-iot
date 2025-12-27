using Artizan.IoT.BatchProcessing.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 批处理核心接口
/// 【设计思路】：定义批处理的核心行为契约，遵循「接口隔离原则」和「依赖倒置原则」
/// 【设计考量】：
/// 1. 入队（生产）和处理（消费）分离，解耦生产消费逻辑
/// 2. 支持分区执行模式动态切换，适配不同业务场景
/// 3. 所有异步方法带CancellationToken，支持优雅取消
/// 【设计模式】：接口契约（无具体模式，是所有批处理器的基础）
/// </summary>
/// <typeparam name="TMessage">消息类型（泛型适配多业务）</typeparam>
public interface IBatchProcessor<TMessage>
{
    /// <summary>
    /// 消息入队（生产端调用）
    /// </summary>
    /// <param name="message">待处理消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>入队结果（Task表示异步完成）</returns>
    Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行批处理（消费端核心逻辑，子类实现）
    /// </summary>
    /// <param name="messages">批量消息列表</param>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="traceId">全链路追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task ProcessBatchAsync(List<TMessage> messages, string partitionKey, string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换分区执行模式（串行/并行）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="newMode">新执行模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换是否成功</returns>
    Task<bool> SwitchPartitionExecutionModeAsync(string partitionKey, ExecutionMode newMode, CancellationToken cancellationToken = default);
}