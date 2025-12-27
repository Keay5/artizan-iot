namespace Artizan.IoT.BatchProcessing.Enums;

/// <summary>
/// 兜底存储类型枚举
/// 【设计思路】：分类标记消息失败场景，便于后续补偿和问题定位
/// 【设计考量】：按消息处理阶段分类，精准定位失败根源
/// 1. 入队失败：生产端问题
/// 2. 隔离失败：并发超限
/// 3. 熔断失败：处理失败次数超限
/// 4. 处理失败：消费端业务逻辑失败
/// 5. 关闭剩余：服务关闭时未处理的消息
/// </summary>
public enum FallbackType
{
    /// <summary>
    /// 消息入队阶段失败
    /// </summary>
    EnqueueFailure,

    /// <summary>
    /// 隔离策略触发（并发数超限）
    /// </summary>
    IsolateFailure,

    /// <summary>
    /// 熔断策略触发（失败次数超限）
    /// </summary>
    CircuitBreakerFailure,

    /// <summary>
    /// 批处理执行阶段失败
    /// </summary>
    ProcessFailure,

    /// <summary>
    /// 服务关闭时的剩余未处理消息
    /// </summary>
    ShutdownRemaining
}