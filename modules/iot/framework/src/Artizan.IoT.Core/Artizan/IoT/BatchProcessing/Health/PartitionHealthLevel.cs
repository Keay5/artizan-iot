namespace Artizan.IoT.BatchProcessing.Health;

/// <summary>
/// 分区健康等级（用于快速判定分区状态）
/// 【设计思路】：按严重程度分级，便于监控告警和自动处理
/// </summary>
public enum PartitionHealthLevel
{
    /// <summary>
    /// 健康（所有指标正常）
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// 警告（部分指标接近阈值，需关注）
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 异常（核心指标超标，影响处理效率）
    /// </summary>
    Abnormal = 2,

    /// <summary>
    /// 熔断（分区已被熔断，停止处理）
    /// </summary>
    CircuitBroken = 3
}