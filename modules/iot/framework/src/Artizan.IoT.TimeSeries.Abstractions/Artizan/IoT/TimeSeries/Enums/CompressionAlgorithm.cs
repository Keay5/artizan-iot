namespace Artizan.IoT.TimeSeries.Enums;

/// <summary>
/// 压缩算法枚举
/// 设计思路：抽象压缩算法类型，支持可插拔切换
/// 设计考量：时序数据量大，压缩能降低存储和传输成本，不同算法权衡压缩率和性能
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// 不压缩
    /// </summary>
    None,

    /// <summary>
    /// Gzip压缩（平衡压缩率和性能）
    /// </summary>
    Gzip,

    /// <summary>
    /// Lz4压缩（时序库首选，高性能）
    /// </summary>
    Lz4,

    /// <summary>
    /// Snappy压缩（Google开源，低延迟）
    /// </summary>
    Snappy
}
