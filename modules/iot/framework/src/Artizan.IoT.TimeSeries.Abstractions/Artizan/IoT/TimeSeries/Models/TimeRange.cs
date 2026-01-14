using System;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 时间范围对象
/// 设计思路：封装时间范围，提供有效性校验
/// 设计考量：时序数据查询必带时间范围，统一校验逻辑避免重复代码
/// </summary>
public record TimeRange(DateTime StartTimeUtc, DateTime EndTimeUtc)
{
    /// <summary>
    /// 验证时间范围是否有效
    /// </summary>
    public bool IsValid => EndTimeUtc > StartTimeUtc;

    /// <summary>
    /// 获取时间范围的时长
    /// </summary>
    public TimeSpan Duration => EndTimeUtc - StartTimeUtc;
}
