using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.ScriptDataCodec.Extensions;

/// <summary>
/// 编解码器扩展方法
/// 设计思路：封装超时控制逻辑，简化上层调用
/// 设计考量：支持自定义超时时间，避免脚本死循环阻塞线程
/// </summary>
public static class CodecExtensions
{
    /// <summary>
    /// 带超时控制的解码方法
    /// </summary>
    public static async Task<ScriptExecutionResult> DecodeWithTimeoutAsync(
        this IDataCodec codec,
        ScriptExecutionContext context,
        TimeSpan? timeout = null)
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(1);
        var decodeTask = codec.DecodeAsync(context);
        var timeoutTask = Task.Delay(timeoutValue);

        var completedTask = await Task.WhenAny(decodeTask, timeoutTask);
        if (completedTask == timeoutTask)
        {
            return ScriptExecutionResult.Fail($"解码超时（超过{timeoutValue.TotalMilliseconds}ms）");
        }

        return await decodeTask;
    }

    /// <summary>
    /// 带超时控制的编码方法
    /// </summary>
    public static async Task<ScriptExecutionResult> EncodeWithTimeoutAsync(
        this IDataCodec codec,
        ScriptExecutionContext context,
        TimeSpan? timeout = null)
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(1);
        var encodeTask = codec.EncodeAsync(context);
        var timeoutTask = Task.Delay(timeoutValue);

        var completedTask = await Task.WhenAny(encodeTask, timeoutTask);
        if (completedTask == timeoutTask)
        {
            return ScriptExecutionResult.Fail($"编码超时（超过{timeoutValue.TotalMilliseconds}ms）");
        }

        return await encodeTask;
    }
}