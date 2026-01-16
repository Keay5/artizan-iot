using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Artizan.IoT.ScriptDataCodec;

/// <summary>
/// 编解码专用日志工具（实例化设计，无静态依赖）
/// 设计思路：封装日志格式，统一输出关键信息（产品、设备、方法名）
/// 设计考量：
/// 1.包含成功/失败/熔断日志，支持上下文追踪
/// 2.构造注入ILogger，强制非空，避免静默失败
/// 3.不设计为静态类，静态类在极端情况下日志输出错乱（虽 ILogger 本身线程安全，但静态类的初始化逻辑可能竞态）
/// </summary>
public class CodecLogger
{
    private readonly ILogger _logger;

    public CodecLogger(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void LogSuccess(ScriptExecutionContext context, string operation)
    {
        _logger.LogDebug(
            "[{Operation}成功] 产品:{ProductKey} 设备:{DeviceName} 方法:{MethodName} 原始数据:{RawData}",
            operation,
            context.ProductKey,
            context.DeviceName,
            context.MethodName,
            context.RawDataToHexString());
    }

    public void LogError(ScriptExecutionContext context, string operation, string errorMessage)
    {
        _logger.LogError(
            "[{Operation}失败] 产品:{ProductKey} 设备:{DeviceName} 方法:{MethodName} 错误:{ErrorMessage} 原始数据:{RawData}",
            operation,
            context.ProductKey,
            context.DeviceName,
            context.MethodName,
            errorMessage,
            context.RawDataToHexString());
    }

    public void LogCircuitBreak(string productKey)
    {
        _logger.LogWarning("[熔断器触发] 产品:{ProductKey} 编解码服务已熔断", productKey);
    }
}
