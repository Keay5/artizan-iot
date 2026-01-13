using Artizan.IoT.TimeSeries.Models;

namespace Artizan.IoT.TimeSeries.InfluxDB;

/// <summary>
/// 时序数据字段常量（全局统一）
/// </summary>
public static class TimeSeriesFieldConsts
{
    /// <summary>
    /// 设备/实体唯一标识（InfluxDB Tag Key）
    /// 全局唯一变量名，所有地方都引用该常量，要求与<see cref="TimeSeriesData.ThingIdentifier"/>保持一致
    /// InfluxDB 会将驼峰命名转换为下划线命名，因此这里使用下划线命名以确保一致性
    /// </summary>
    public const string ThingIdentifier = "thing_identifier";

    // InfluxDB 特有的核心字段常量
    public const string Measurement = "_measurement";
    public const string Time = "_time";
    public const string Field = "_field";
    public const string Value = "_value";
}
