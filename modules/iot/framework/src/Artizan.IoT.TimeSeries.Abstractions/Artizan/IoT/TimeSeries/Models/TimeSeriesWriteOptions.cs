using Artizan.IoT.TimeSeries.Enums;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 时序数据写入选项
/// 设计思路：封装写入行为配置，支持精细化控制写入策略
/// 设计模式：选项模式（Options Pattern），适配.NET配置系统
/// 设计考量：
/// 1. 支持幂等写入，避免重复数据
/// 2. 可配置压缩策略，平衡性能和存储
/// 3. 区分一致性级别，适配不同业务场景
/// 4. 批量写入阈值，支持本地缓存批量提交
/// </summary>
public class TimeSeriesWriteOptions
{
    /// <summary>
    /// 是否启用幂等写入（通过 ThingIdentifier + UtcDateTime 去重）
    /// </summary>
    public bool EnableIdempotency { get; set; } = true;

    /// <summary>
    /// 是否启用数据压缩
    /// </summary>
    public bool EnableCompression { get; set; } = TimeSeriesConsts.DefaultCompressionEnabled;

    /// <summary>
    /// 压缩算法
    /// </summary>
    public CompressionAlgorithm CompressionAlgorithm { get; set; } = TimeSeriesConsts.DefaultCompressionAlgorithm;

    /// <summary>
    /// 压缩级别（1-9，越高压缩率越高但耗时越长）
    /// </summary>
    public int CompressionLevel { get; set; } = TimeSeriesConsts.DefaultCompressionLevel;

    /// <summary>
    /// 写入一致性级别
    /// </summary>
    public TimeSeriesConsistencyLevel ConsistencyLevel { get; set; } = TimeSeriesConsistencyLevel.Eventual;

    /// <summary>
    /// 批量写入阈值（小于该值时本地缓存，达到阈值后批量提交）
    /// </summary>
    public int BatchThreshold { get; set; } = TimeSeriesConsts.DefaultBatchThreshold;

    /// <summary>
    /// 写入超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
