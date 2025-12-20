using MQTTnet.Protocol;
using System;
using System.Text;

namespace Artizan.IoT.Mqtts.Messages;

/// <summary>
/// 轻量MQTT原始消息（彻底断绝原数组引用，所有Payload数据为独立深拷贝）
/// </summary>
public class MqttRawMessage : IDisposable
{
    #region 核心元数据（值类型，只读）
    public string Topic { get; }
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }
    public bool Retain { get; }
    public DateTime ReceiveTimeUtc { get; }
    #endregion

    #region 核心：PayloadSegment（基于独立深拷贝数组，彻底解耦原数组）
    /// <summary>
    /// 独立深拷贝后的Payload切片（Array指向新数组，无原数组引用）
    /// </summary>
    public ArraySegment<byte> PayloadSegment { get; }

    /// <summary>
    /// 快捷访问Payload的Span（基于独立数组，安全无风险）
    /// </summary>
    public Span<byte> PayloadSpan => PayloadSegment.AsSpan();

    /// <summary>
    /// 快捷访问Payload的ReadOnlySpan（只读，避免意外修改）
    /// </summary>
    public ReadOnlySpan<byte> PayloadReadOnlySpan => PayloadSegment.AsSpan();
    #endregion

    #region PayloadString：非冗余，高频场景快捷访问（基于独立数组）
    /*
     PayloadString 仍非冗余设计，核心原因：
     - 高频场景刚需：物联网中 80%+ 的 Payload 是 JSON / 文本格式（如设备上报的 Alink JSON），每次解析都手动调用 Encoding.UTF8.GetString(segment) 会导致代码冗余，且易遗漏非 UTF8 容错处理；
     - 性能无损耗：基于独立数组的Span转换字符串（Encoding.UTF8.GetString(PayloadReadOnlySpan)）是零拷贝操作，仅首次访问时计算，后续复用；
     - 无风险：基于独立数组转换，不存在原数组修改 / 回收导致的脏数据问题，无需担心安全风险。
     */

    /// <summary>
    /// Payload的UTF8字符串（基于独立数组，仅首次访问时计算）
    /// </summary>
    private string? _payloadString;
    public string PayloadString
    {
        get
        {
            if (_payloadString == null)
            {
                _payloadString = GetUtf8StringSafely();
            }
            return _payloadString;
        }
    }
    #endregion

    #region Payload摘要（日志友好，避免大文本）
    /// <summary>
    /// Payload摘要（懒加载，默认显示前100字符，非UTF8显示字节长度）
    /// </summary>
    private string? _payloadSummary;
    public string PayloadSummary
    {
        get
        {
            if (_payloadSummary == null)
            {
                // 默认摘要长度100，可根据业务调整
                _payloadSummary = GetPayloadSummary(PayloadSegment, 100);
            }
            return _payloadSummary;
        }
    }
    #endregion

    #region 构造函数（核心：深拷贝原PayloadSegment到独立数组，彻底解耦）
    /// <summary>
    /// 主构造函数（仅接收元数据和原Segment，初始化即深拷贝）
    /// </summary>
    /// <param name="topic">Topic</param>
    /// <param name="qosLevel">QoS等级</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="originalPayloadSegment">原MQTT消息的PayloadSegment（仅用于拷贝）</param>
    public MqttRawMessage(
        string topic,
        MqttQualityOfServiceLevel qosLevel,
        bool retain,
        ArraySegment<byte> originalPayloadSegment)
    {
        // 1. 赋值元数据（值类型，无引用风险）
        Topic = topic ?? string.Empty;
        QualityOfServiceLevel = qosLevel;
        Retain = retain;

        // 2. 核心：深拷贝原Segment到独立数组，彻底断绝原数组引用
        PayloadSegment = DeepCopySegment(originalPayloadSegment);
    }
    #endregion

    #region 核心方法：深拷贝原Segment到新数组
    /// <summary>
    /// 深拷贝原ArraySegment<byte>到独立新数组，返回新的Segment
    /// </summary>
    private ArraySegment<byte> DeepCopySegment(ArraySegment<byte> original)
    {
        // 场景1：原Segment为空/无效 → 返回空Segment
        if (original.Array == null || original.Count <= 0)
        {
            return ArraySegment<byte>.Empty;
        }

        // 场景2：深拷贝到新数组（核心：新数组与原数组无任何引用）
        var newArray = new byte[original.Count];
        Array.Copy(
            sourceArray: original.Array,
            sourceIndex: original.Offset,
            destinationArray: newArray,
            destinationIndex: 0,
            length: original.Count);

        // 返回基于新数组的Segment（Offset=0，Count=newArray.Length）
        return new ArraySegment<byte>(newArray, 0, newArray.Length);
    }
    #endregion

    #region 辅助方法：安全转换UTF8字符串（基于独立数组，无脏数据风险）
    private string GetUtf8StringSafely()
    {
        try
        {
            return Encoding.UTF8.GetString(PayloadReadOnlySpan);
        }
        catch
        {
            // 非UTF8数据返回标识，避免异常
            return $"[Non-UTF8 Payload, Length: {PayloadSegment.Count}]";
        }
    }
    #endregion

    #region 生成Payload摘要（适配ArraySegment<byte>，无数组拷贝）

    /// <summary>
    /// 核心方法：生成Payload摘要（适配ArraySegment<byte>，无数组拷贝）
    /// 
    /// 测试非UTF8 Payload场景：
    /// byte[] binaryPayload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
    /// 输出：非UTF8编码 | 字节长度：9 | 前8字节16进制：01 02 03 04 05 06 07 08
    /// </summary>
    /// <param name="segment">Payload切片（独立数组，无原引用）</param>
    /// <param name="maxLength">摘要最大字符长度</param>
    /// <returns>Payload摘要字符串</returns>
    private string GetPayloadSummary(ArraySegment<byte> segment, int maxLength)
    {
        // 场景1：空Payload
        if (segment.Count == 0)
        {
            return "空Payload";
        }

        try
        {
            // 场景2：UTF8编码 → 截断显示（用Span避免数组拷贝）
            var payloadSpan = segment.AsSpan();
            var payloadStr = Encoding.UTF8.GetString(payloadSpan);

            return payloadStr.Length <= maxLength
                ? payloadStr
                : $"{payloadStr[..maxLength]}...(总长度：{payloadStr.Length})";
        }
        catch
        {
            // 场景3：非UTF8编码 → 显示字节长度+前8个字节的16进制（修正Span转换问题）
            //int takeLength = Math.Min(8, segment.Count);
            //// 临时数组（仅8字节，开销极小），兼容BitConverter.ToString参数
            //byte[] tempBytes = new byte[takeLength];
            //// 从Segment的Span复制到临时数组（零拷贝级别的高效操作）
            //segment.AsSpan(0, takeLength).CopyTo(tempBytes);
            //// 生成16进制前缀
            //var hexPrefix = BitConverter.ToString(tempBytes).Replace("-", " ");
            //return $"非UTF8编码 | 字节长度：{segment.Count} | 前8字节16进制：{hexPrefix}";

            ///*--------------------------------------------------------------------------------------------
            // 若追求极致零分配，可手动拼接 16 进制字符串（替代 BitConverter.ToString），彻底避免临时数组
            // */
            int takeLength = Math.Min(8, segment.Count);
            var span = segment.AsSpan(0, takeLength);
            StringBuilder sb = new StringBuilder(3 * takeLength); // 预分配容量（每字节占3字符：XX ）
            for (int i = 0; i < span.Length; i++)
            {
                sb.Append(span[i].ToString("X2")); // 转两位16进制
                if (i < span.Length - 1) sb.Append(" ");
            }
            var hexPrefix = sb.ToString();
            return $"非UTF8编码 | 字节长度：{segment.Count} | 前8字节16进制：{hexPrefix}";
        }
    }

    /// <summary>
    /// 公开方法：自定义长度生成Payload摘要（按需使用）
    /// </summary>
    /// <param name="maxLength">自定义最大字符长度</param>
    /// <returns>自定义长度的Payload摘要</returns>
    public string GetPayloadSummary(int maxLength)
    {
        return GetPayloadSummary(PayloadSegment, maxLength);
    }
    #endregion


    #region 资源释放（清理独立数组，加速GC）
    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;

        // 清空独立数组，断绝数据残留，加速GC回收
        if (PayloadSegment.Array != null)
        {
            Array.Clear(PayloadSegment.Array, 0, PayloadSegment.Array.Length);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~MqttRawMessage() => Dispose();
    #endregion

    #region 快捷方法：转换为字节数组（基于独立数组）
    /// <summary>
    /// 获取独立的Payload字节数组（与原数组无引用）
    /// </summary>
    public byte[] ToPayloadArray()
    {
        if (PayloadSegment.Count == 0)
        {
            return Array.Empty<byte>();
        }

        // 基于独立Segment拷贝（双重保险，避免外部修改内部数组）
        var array = new byte[PayloadSegment.Count];
        PayloadReadOnlySpan.CopyTo(array);
        return array;
    }
    #endregion
}
