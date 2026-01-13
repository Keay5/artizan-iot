namespace Artizan.IoT.TimeSeries.Enums;

/// <summary>
/// 时序数据聚合类型枚举
/// 设计思路：标准化聚合操作类型，避免字符串硬编码
/// 设计考量：适配不同时序库的聚合函数，统一上层调用接口
/// </summary>
public enum TimeSeriesAggregateType
{
    /// <summary>
    /// 求和
    /// </summary>
    Sum,

    /// <summary>
    /// 平均值
    /// </summary>
    Avg,

    /// <summary>
    /// 最大值
    /// </summary>
    Max,

    /// <summary>
    /// 最小值
    /// </summary>
    Min,

    /// <summary>
    /// 计数
    /// </summary>
    Count,

    /// <summary>
    /// 第一条数据
    /// </summary>
    First,

    /// <summary>
    /// 最后一条数据
    /// </summary>
    Last
}
