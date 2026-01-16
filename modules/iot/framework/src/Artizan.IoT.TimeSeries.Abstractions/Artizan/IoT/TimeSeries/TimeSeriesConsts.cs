using Artizan.IoT.TimeSeries.Enums;

namespace Artizan.IoT.TimeSeries;

/// <summary>
/// 时序数据常量定义
/// 设计思路：集中管理魔法值，提升代码可维护性
/// 设计考量：避免硬编码，统一默认值配置，便于全局修改
/// </summary>
public static class TimeSeriesConsts
{
    /// <summary>
    /// 默认测量表名
    /// </summary>
    public const string DefaultMeasurementName = "iot_thing_telemetry";

    /// <summary>
    /// 默认查询条数限制
    /// </summary>
    public const int DefaultQueryLimit = 1000;

    /// <summary>
    /// 默认时间窗口（1分钟）
    /// </summary>
    public const string DefaultTimeWindow = "1m";

    /// <summary>
    /// 默认是否启用压缩
    /// </summary>
    public const bool DefaultCompressionEnabled = true;

    /// <summary>
    /// 默认压缩级别（Gzip中等压缩）
    /// </summary>
    public const int DefaultCompressionLevel = 6;

    /// <summary>
    /// 默认压缩算法（Lz4：时序库高性能压缩）
    /// </summary>
    public const CompressionAlgorithm DefaultCompressionAlgorithm = CompressionAlgorithm.Lz4;

    /// <summary>
    /// 默认批量写入阈值
    /// </summary>
    public const int DefaultBatchThreshold = 1000;
}
