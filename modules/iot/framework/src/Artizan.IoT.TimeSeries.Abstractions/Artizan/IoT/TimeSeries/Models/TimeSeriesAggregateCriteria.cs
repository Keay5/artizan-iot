using Artizan.IoT.TimeSeries.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 时序数据聚合查询条件
/// 设计思路：继承基础查询条件，扩展聚合相关参数
/// 设计模式：继承+扩展，保持聚合查询和基础查询的一致性
/// 设计考量：聚合查询是基础查询的扩展，复用基础参数，减少代码冗余
/// </summary>
public class TimeSeriesAggregateCriteria : TimeSeriesQueryCriteria
{
    /// <summary>
    /// 聚合类型（必填）
    /// </summary>
    [Required(ErrorMessage = "聚合类型不能为空")]
    public TimeSeriesAggregateType AggregateType { get; set; }

    /// <summary>
    /// 聚合字段名（必填）
    /// </summary>
    [Required(ErrorMessage = "聚合字段名不能为空")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 时间窗口（如 1m/5m/1h，遵循时序库标准格式）
    /// </summary>
    [Required(ErrorMessage = "聚合时间窗口不能为空")]
    public string TimeWindow { get; set; } = TimeSeriesConsts.DefaultTimeWindow;

    /// <summary>
    /// 验证聚合查询条件是否有效
    /// </summary>
    /// <exception cref="ArgumentException">条件无效时抛出</exception>
    public new void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(FieldName))
        {
            throw new ArgumentException("聚合字段名不能为空", nameof(FieldName));
        }

        if (string.IsNullOrWhiteSpace(TimeWindow))
        {
            throw new ArgumentException("聚合时间窗口不能为空", nameof(TimeWindow));
        }
    }
}
