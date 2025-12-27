using System;
using System.Buffers;

namespace Artizan.IoT.BatchProcessing.Core;

/// <summary>
/// 带时间戳的消息对象（池化复用）
/// 【设计思路】：对象池模式，减少高并发下的GC压力
/// 【设计考量】：
/// 1. 封装消息体、时间戳、TraceId、MessageId等核心字段
/// 2. 提供Reset方法，归还池时清空数据，避免数据残留
/// 3. 字节数组使用ArrayPool复用，进一步优化内存
/// </summary>
/// <typeparam name="T">消息体类型</typeparam>
public class TimedMessage<T>
{
    /// <summary>
    /// 消息体
    /// </summary>
    public T Payload { get; set; }

    /// <summary>
    /// 入队时间（UTC）
    /// </summary>
    public DateTime EnqueueTimeUtc { get; set; }

    /// <summary>
    /// 全链路追踪ID
    /// </summary>
    public string TraceId { get; set; }

    /// <summary>
    /// 消息唯一ID
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// 消息二进制数据（用于序列化存储）
    /// </summary>
    public byte[] BinaryData { get; set; }

    /// <summary>
    /// 重置对象（归还池时调用）
    /// 【设计思路】：清空所有字段，避免复用导致的数据污染
    /// </summary>
    public void Reset()
    {
        Payload = default;
        EnqueueTimeUtc = DateTime.MinValue;
        TraceId = null;
        MessageId = null;

        // 归还字节数组到ArrayPool，减少内存分配
        if (BinaryData != null)
        {
            ArrayPool<byte>.Shared.Return(BinaryData);
            BinaryData = null;
        }
    }
}
