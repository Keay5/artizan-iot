using Artizan.IoT.BatchProcessing.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;


/// <summary>
/// 执行顺序策略接口
/// 【设计思路】：控制消息处理的执行顺序（串行/并行）
/// 【设计考量】：
/// 1. 支持动态切换执行模式，适配不同业务的一致性要求
/// 2. 切换时支持超时控制，避免死锁
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IExecutionOrderStrategy
{
    /// <summary>
    /// 按指定模式执行异步操作
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="operation">待执行操作</param>
    /// <param name="mode">执行模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<TResult> ExecuteAsync<TResult>(
        string partitionKey,
        Func<CancellationToken, Task<TResult>> operation,
        ExecutionMode mode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换指定分区的执行模式
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <param name="newMode">新执行模式</param>
    /// <param name="timeout">切换超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>切换是否成功</returns>
    Task<bool> ChangeExecutionModeAsync(
        string partitionKey,
        ExecutionMode newMode,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}