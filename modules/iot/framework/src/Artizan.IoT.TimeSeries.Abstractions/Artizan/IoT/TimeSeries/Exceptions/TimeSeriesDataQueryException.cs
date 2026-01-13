using System;

namespace Artizan.IoT.TimeSeries.Exceptions;

/// <summary>
/// 时序数据查询异常
/// 设计思路：区分查询异常类型，便于针对性处理（如超时重试、降级）
/// </summary>
public class TimeSeriesDataQueryException : TimeSeriesDataException
{
    public TimeSeriesDataQueryException(string message) : base(message)
    {
    }

    public TimeSeriesDataQueryException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public TimeSeriesDataQueryException(string message, string thingIdentifier, Exception innerException) : base(message, thingIdentifier, innerException)
    {
    }
}
