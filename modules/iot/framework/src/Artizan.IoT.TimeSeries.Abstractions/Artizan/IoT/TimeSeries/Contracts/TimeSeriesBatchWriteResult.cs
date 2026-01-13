using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 批量写入结果
/// 设计思路：批量操作需区分成功/失败条数，便于监控和重试
/// 设计考量：
/// 1. 统计总条数、成功条数、失败条数
/// 2. 记录错误信息，便于定位失败原因
/// 3. 批量写入时间，用于性能分析
/// </summary>
public class TimeSeriesBatchWriteResult
{
    /// <summary>
    /// 总提交条数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功写入条数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败条数
    /// </summary>
    public int FailedCount => TotalCount - SuccessCount;

    /// <summary>
    /// 错误消息列表（失败时非空）
    /// </summary>
    public IList<string> ErrorMessages { get; set; } = new List<string>();

    /// <summary>
    /// 批量写入完成时间（UTC）
    /// </summary>
    public DateTime BatchWrittenTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 批量ID（用于追踪整个批量操作）
    /// </summary>
    public string BatchId { get; set; } = Guid.NewGuid().ToString();
}
