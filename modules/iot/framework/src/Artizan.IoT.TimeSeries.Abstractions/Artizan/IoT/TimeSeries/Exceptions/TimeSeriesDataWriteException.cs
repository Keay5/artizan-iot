using System;

namespace Artizan.IoT.TimeSeries.Exceptions;

/// <summary>
/// 时序数据写入异常
/// 设计思路：区分写入异常类型，便于针对性处理（如重试、告警）
/// </summary>
public class TimeSeriesDataWriteException : TimeSeriesDataException
{
    public TimeSeriesDataWriteException(string message) : base(message)
    {
    }

    public TimeSeriesDataWriteException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public TimeSeriesDataWriteException(string message, string thingIdentifier, Exception innerException) : base(message, thingIdentifier, innerException)
    {
    }
}
