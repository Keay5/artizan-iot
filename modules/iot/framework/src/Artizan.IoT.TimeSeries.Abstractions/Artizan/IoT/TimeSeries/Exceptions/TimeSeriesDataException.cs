using System;

namespace Artizan.IoT.TimeSeries.Exceptions;

/// <summary>
/// 时序数据操作异常基类
/// 设计思路：自定义异常体系，便于上层精准捕获和处理
/// 设计模式：异常分层设计，基类统一处理通用逻辑，子类区分具体异常类型
/// 设计考量：避免捕获通用Exception，提升异常处理的精准性和可维护性
/// </summary>
public class TimeSeriesDataException : Exception
{
    /// <summary>
    /// 异常关联的物标识
    /// </summary>
    public string? ThingIdentifier { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    public TimeSeriesDataException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public TimeSeriesDataException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="thingIdentifier">关联的物标识</param>
    /// <param name="innerException">内部异常</param>
    public TimeSeriesDataException(string message, string thingIdentifier, Exception innerException) : base(message, innerException)
    {
        ThingIdentifier = thingIdentifier;
    }
}
