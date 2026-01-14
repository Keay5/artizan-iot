using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 时序数据查询条件
/// 设计思路：使用强类型对象封装查询参数，替代零散参数
/// 设计模式：参数对象模式（Parameter Object）
/// 设计考量：
/// 1. 减少方法参数数量，提升代码可读性
/// 2. 便于扩展新的查询条件，无需修改方法签名
/// 3. 内置参数校验，提前发现非法查询条件
/// </summary>
public class TimeSeriesQueryCriteria
{
    /// <summary>
    /// 物唯一标识（必填）
    /// </summary>
    [Required(ErrorMessage = "物唯一标识不能为空")]
    public string ThingIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 时间范围（必填）
    /// </summary>
    [Required(ErrorMessage = "查询时间范围不能为空")]
    public TimeRange TimeRange { get; set; } = new TimeRange(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

    /// <summary>
    /// 测量表名（为空时使用默认值）
    /// </summary>
    public string? Measurement { get; set; }

    /// <summary>
    /// 要查询的字段列表（为空则查询所有字段）
    /// </summary>
    public IList<string> FieldNames { get; set; } = new List<string>();

    /// <summary>
    /// 标签筛选条件（时序库索引查询，高性能）
    /// </summary>
    public IDictionary<string, string> TagFilters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 数据条数限制（防止海量数据查询）
    /// </summary>
    public int Limit { get; set; } = TimeSeriesConsts.DefaultQueryLimit;

    /// <summary>
    /// 是否按时间戳降序排列
    /// </summary>
    public bool OrderByDescending { get; set; } = true;

    /// <summary>
    /// 验证查询条件是否有效
    /// </summary>
    /// <exception cref="ArgumentException">条件无效时抛出</exception>
    public void Validate()
    {
        if (!TimeRange.IsValid)
        {
            throw new ArgumentException("查询时间范围无效，结束时间必须大于开始时间", nameof(TimeRange));
        }

        if (Limit <= 0 || Limit > 10000)
        {
            throw new ArgumentException($"查询条数限制必须在1-10000之间，当前值：{Limit}", nameof(Limit));
        }
    }
}
