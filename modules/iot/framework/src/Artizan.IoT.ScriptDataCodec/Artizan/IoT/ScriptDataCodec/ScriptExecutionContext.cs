using System;
using System.Text;

namespace Artizan.IoT.ScriptDataCodec;

/// <summary>
/// 脚本执行上下文
/// 设计思路：封装所有执行所需参数，支持动态方法名，提升扩展性
/// 设计考量：
/// 1. 包含原始数据、协议数据、产品/设备标识（用于日志追踪）
/// 2. 新增MethodName字段，支持调用脚本中任意自定义方法
/// 3. 提供RawData转16进制扩展方法，方便日志输出
/// </summary>
public class ScriptExecutionContext
{
    /// <summary>
    /// 设备上报的原始二进制数据（Decode时必填）
    /// </summary>
    public byte[] RawData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 标准协议数据（如JSON字符串，Encode时必填）
    /// </summary>
    public string ProtocolData { get; set; } = string.Empty;

    /// <summary>
    /// 要调用的脚本方法名（可选，默认decode/encode）
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// 产品唯一标识（用于对象池隔离）
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（用于日志追踪）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 原始数据转16进制字符串（日志专用）
    /// </summary>
    public string RawDataToHexString()
    {
        if (RawData.Length == 0)
        {
            return string.Empty;
        }
        var sb = new StringBuilder();
        foreach (byte b in RawData)
        {
            _ = sb.AppendFormat("{0:X2} ", b);
        }
        return sb.ToString().TrimEnd();
    }
}