namespace Artizan.IoT.TimeSeries.Enums;

/// <summary>
/// 时序数据一致性级别
/// 设计思路：适配不同业务场景的一致性要求
/// 设计考量：IoT场景下写多读少，区分最终一致性（高吞吐）和强一致性（核心数据）
/// </summary>
public enum TimeSeriesConsistencyLevel
{
    /// <summary>
    /// 最终一致性（高吞吐，适合非核心数据）
    /// </summary>
    Eventual,

    /// <summary>
    /// 强一致性（写入成功即持久化，适合核心数据）
    /// </summary>
    Strong
}
