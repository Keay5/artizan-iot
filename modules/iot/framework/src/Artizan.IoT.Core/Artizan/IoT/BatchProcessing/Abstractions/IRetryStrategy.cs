using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 重试策略接口
/// 【设计思路】：失败自动重试，提升消息处理成功率
/// 【设计考量】：
/// 1. 泛型支持不同返回值类型，适配多场景
/// 2. 可配置重试次数和间隔，适配不同业务的重试需求
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IRetryStrategy
{
    /// <summary>
    /// 执行带重试的异步操作
    /// </summary>
    /// <typeparam name="TResult">操作返回值类型</typeparam>
    /// <param name="operation">待执行的异步操作</param>
    /// <param name="partitionKey">分区标识（便于关联重试上下文）</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="retryInterval">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        string partitionKey,
        int maxRetryCount,
        TimeSpan retryInterval,
        CancellationToken cancellationToken = default);
}