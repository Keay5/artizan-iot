using System;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 单条写入结果
/// 设计思路：结构化返回写入结果，包含丰富的状态信息
/// 设计考量：
/// 1. 明确写入是否成功
/// 2. 标记重复数据（幂等写入场景）
/// 3. 记录写入时间，便于问题排查
/// </summary>
public class TimeSeriesWriteResult
{
    /// <summary>
    /// 是否写入成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（失败时非空）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 存储引擎生成的唯一ID（不同引擎类型不同）
    /// </summary>
    public string DataPointId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 实际写入时间（UTC）
    /// </summary>
    public DateTime WrittenTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否为幂等去重的重复数据
    /// </summary>
    public bool IsDuplicate { get; set; }
}
