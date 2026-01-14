using Artizan.IoT.Localization;
using Artizan.IoT.Results;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Exceptions;

/// <summary>
/// IoTResultException全局异常处理器（ABP集成）
/// 设计模式：全局异常处理模式（Global Exception Handling）
/// 设计思路：
/// 1. 统一处理IoTResultException，返回标准化错误响应
/// 2. 集成ABP本地化框架，返回多语言错误消息
/// 3. 隐藏底层异常细节，仅返回用户友好的错误信息
/// 设计考量：
/// - 标准化响应格式：符合RESTful API设计规范，便于前端统一处理
/// - 安全：不暴露敏感的异常堆栈信息给客户端
/// - 可扩展：可根据错误码配置不同的HTTP状态码
/// - 日志：可在此处补充异常日志，便于问题排查
/// </summary>
//public class IoTExceptionFilter : IExceptionFilter
//{
//    private readonly IExceptionToErrorInfoConverter _errorInfoConverter;
//    private readonly IStringLocalizer<IoTResource> _localizer;
//    private readonly ILogger<IoTExceptionFilter> _logger;

//    public IoTExceptionFilter(
//        IExceptionToErrorInfoConverter errorInfoConverter,
//        IStringLocalizer<IoTResource> localizer,
//        ILogger<IoTExceptionFilter> logger)
//    {
//        _errorInfoConverter = errorInfoConverter;
//        _localizer = localizer;
//        _logger = logger;
//    }

//    public void OnException(ExceptionContext context)
//    {
//        if (context.Exception is not IoTResultException ex)
//        {
//            return;
//        }

//        // 记录异常日志（包含完整上下文）
//        _logger.LogError(ex, "IoTResultException occurred | ErrorCodes: {ErrorCodes}",
//            ex.IoTResult.Errors.Select(e => e.Code).JoinAsString(","));

//        // 本地化异常消息
//        var localizedMessage = ex.IoTResult.LocalizeErrors(_localizer);

//        // 构建标准化错误响应（符合ABP RemoteServiceErrorResponse格式）
//        var errorInfo = new RemoteServiceErrorInfo
//        {
//            Code = ex.Code,
//            Message = localizedMessage,
//            Details = ex.IoTResult.Errors.Select(e => e.ToString()).ToList()
//        };

//        // 设置响应结果（400 Bad Request，可根据错误码调整）
//        context.Result = new ObjectResult(new RemoteServiceErrorResponse(errorInfo))
//        {
//            StatusCode = StatusCodes.Status400BadRequest
//        };
//        context.ExceptionHandled = true;
//    }
//}
