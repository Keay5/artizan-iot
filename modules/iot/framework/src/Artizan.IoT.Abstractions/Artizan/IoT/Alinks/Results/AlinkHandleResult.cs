using Artizan.IoT.Alinks.DataObjects;
using Artizan.IoT.Commons.Tracing;

namespace Artizan.IoT.Alinks.Results;

/// <summary>
/// AlinkJson解析+TSL校验结果（封装模式，适配ABP社区版）
/// 【设计思路】
/// 1. 核心职责：结构化封装解析/校验结果，替代简单的bool+string，提升代码可读性；
/// 2. 设计原则：
///    - 封装变化：将结果状态（成功/失败）、错误信息、TraceId等封装为统一对象；
///    - 易用性：提供静态工厂方法（Success/ParseFailed），简化结果创建；
/// 3. 核心考量：
///    - 可观测性：包含TraceId/原始消息，便于日志排查；
///    - 结构化：错误码+错误信息，便于前端/下游系统处理。
/// 【设计模式】：封装模式（Encapsulation）+ 工厂方法模式
/// - 工厂方法：静态方法创建不同状态的结果对象，隐藏复杂的初始化逻辑。
/// </summary>
public class AlinkHandleResult : IHasTraceId
{
    /// <summary>
    /// 是否处理成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 结果描述（成功/失败信息）
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误码（结构化错误标识，如ALINK_PARSE_FAILED）
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 全链路TraceId（实现IHasTraceId）
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 原始消息内容（失败时用于排查）
    /// </summary>
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>
    /// 标准化消息DTO（成功时赋值）
    /// </summary>
    public AlinkDataContext? AlinkDataContext { get; set; }

    /// <summary>
    /// 工厂方法：创建解析/校验成功结果
    /// </summary>
    public static AlinkHandleResult Success(AlinkDataContext dataContext)
    {
        return new AlinkHandleResult
        {
            IsSuccess = true,
            TraceId = dataContext.TraceId,
            Message = "解析+校验成功",
            AlinkDataContext = dataContext,
           // RawMessage = dataContext.RawMessage
        };
    }

    /// <summary>
    /// 工厂方法：创建解析失败结果
    /// </summary>
    public static AlinkHandleResult ParseFailed(string traceId, string errorMsg, string rawMessage)
    {
        return new AlinkHandleResult
        {
            IsSuccess = false,
            TraceId = traceId,
            Message = $"解析失败：{errorMsg}",
            ErrorCode = "ALINK_PARSE_FAILED",
            RawMessage = rawMessage
        };
    }

    /// <summary>
    /// 工厂方法：创建TSL校验失败结果
    /// </summary>
    public static AlinkHandleResult TslValidateFailed(string traceId, string errorMsg, AlinkDataContext dataContext)
    {
        return new AlinkHandleResult
        {
            IsSuccess = false,
            TraceId = traceId,
            Message = $"TSL校验失败：{errorMsg}",
            ErrorCode = "TSL_VALIDATE_FAILED",
            AlinkDataContext = dataContext,
            //RawMessage = dataContext.RawMessage
        };
    }
}