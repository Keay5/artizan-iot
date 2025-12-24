using Newtonsoft.Json;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

/// <summary>
/// 数值类型规格，适用于：int、float、double
/// </summary>
public class NumericSpecs : ISpecs
{
    [JsonProperty("min", Order = 1)]
    public string Min { get; set; }

    [JsonProperty("max", Order = 2)]
    public string Max { get; set; }

    [JsonProperty("step", Order = 3)]
    public string? Step { get; set; }

    [JsonProperty("unit", Order = 4)]
    public string? Unit { get; set; }

    [JsonProperty("unitName", Order = 5)]
    public string? UnitName { get; set; }

    protected NumericSpecs()
    {
    }

    /// <summary>
    /// 初始化数值规格，构造时校验合法性
    /// </summary>
    public NumericSpecs(string min, string max, string? step = null, string? unit = null, string? unitName = null)
    {
        SetMin(min, max);
        Step = step;
        Unit = unit;
        UnitName = unitName;
    }

    public NumericSpecs SetMin(string min, string max)
    {
        // 校验min和max的数值有效性
        if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max) &&
            double.TryParse(min, out var minVal) && double.TryParse(max, out var maxVal) &&
            minVal > maxVal)
        {
            throw new ArgumentException($"数值规格无效：最小值{min}不能大于最大值{max}");
        }

        Min = min;
        Max = max;

        return this;
    }

}