using System;
using System.ComponentModel.DataAnnotations;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 时序数据删除条件
/// 设计思路：极简设计，仅包含删除必需的参数
/// 设计考量：时序数据建议归档而非删除，限制删除条件，降低误删风险
/// </summary>
public class TimeSeriesDeleteCriteria
{
    /// <summary>
    /// 物唯一标识（必填）
    /// </summary>
    [Required(ErrorMessage = "物唯一标识不能为空")]
    public string ThingIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 时间范围（必填）
    /// </summary>
    [Required(ErrorMessage = "删除时间范围不能为空")]
    public TimeRange TimeRange { get; set; } = new TimeRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

    /// <summary>
    /// 测量表名（为空时使用默认值）
    /// </summary>
    public string? Measurement { get; set; }

    /// <summary>
    /// 验证删除条件是否有效
    /// </summary>
    /// <exception cref="ArgumentException">条件无效时抛出</exception>
    public void Validate()
    {
        if (!TimeRange.IsValid)
        {
            throw new ArgumentException("删除时间范围无效，结束时间必须大于开始时间", nameof(TimeRange));
        }

        // 限制删除时间范围，最大不超过30天，降低误删风险
        if (TimeRange.Duration > TimeSpan.FromDays(30))
        {
            throw new ArgumentException("删除时间范围不能超过30天，如需删除更多数据请联系管理员", nameof(TimeRange));
        }
    }
}

