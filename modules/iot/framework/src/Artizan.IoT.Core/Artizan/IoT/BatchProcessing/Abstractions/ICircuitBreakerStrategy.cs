namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 熔断策略接口
/// 【设计思路】：实现熔断器模式，防止失败扩散导致服务雪崩
/// 【设计考量】：
/// 1. 失败次数达阈值时打开熔断器，快速失败
/// 2. 成功处理时重置熔断状态，支持自动恢复
/// 【设计模式】：熔断器模式（Circuit Breaker Pattern）+ 策略模式
/// </summary>
public interface ICircuitBreakerStrategy
{
    /// <summary>
    /// 判断指定分区的熔断器是否打开
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    /// <returns>true=打开（快速失败），false=关闭（正常处理）</returns>
    bool IsOpen(string partitionKey);

    /// <summary>
    /// 记录一次处理失败（累计失败次数）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    void RecordFailure(string partitionKey);

    /// <summary>
    /// 记录一次处理成功（重置失败次数）
    /// </summary>
    /// <param name="partitionKey">分区标识</param>
    void RecordSuccess(string partitionKey);
}