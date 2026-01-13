namespace Artizan.IoT.ScriptDataCodec;

/// <summary>
/// 脚本执行结果封装
/// 设计思路：统一返回格式，简化上层判断逻辑，区分成功/失败状态
/// 设计考量：
/// 1. 包含成功状态、错误信息
/// 2. 分OutputProtocolData（解码结果）和OutputRawData（编码结果）
/// 3. 提供静态工厂方法，快速创建结果
/// </summary>
public class ScriptExecutionResult
{
    /// <summary>
    /// 执行是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息（失败时非空）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 解码输出：标准协议数据（如JSON）
    /// </summary>
    public string? OutputProtocolData { get; set; }

    /// <summary>
    /// 编码输出：原始二进制数据
    /// </summary>
    public byte[]? OutputRawData { get; set; }

    /// <summary>
    /// 创建解码成功结果
    /// </summary>
    public static ScriptExecutionResult SuccessWithProtocol(string protocolData)
    {
        return new ScriptExecutionResult
        {
            Success = true,
            OutputProtocolData = protocolData
        };
    }

    /// <summary>
    /// 创建编码成功结果
    /// </summary>
    public static ScriptExecutionResult SuccessWithRaw(byte[] rawData)
    {
        return new ScriptExecutionResult
        {
            Success = true,
            OutputRawData = rawData
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ScriptExecutionResult Fail(string errorMessage)
    {
        return new ScriptExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
