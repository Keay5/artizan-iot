using Artizan.IoT.Tracing;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;

namespace Artizan.IoT.BatchProcessing.Core;

/// <summary>
/// 消息对象池管理器（静态封装）
/// 【设计思路】：简化对象池使用，提供便捷的Get/Return方法
/// 【设计考量】：
/// 1. 静态构造函数初始化池，支持环境变量配置池大小
/// 2. 提供重载Get方法，支持直接初始化对象字段
/// 【设计模式】：对象池模式 + 静态工厂
/// </summary>
/// <typeparam name="T">消息体类型</typeparam>
public static class TimedMessagePool<T>
{
    /// <summary>
    /// 静态对象池实例
    /// </summary>
    private static readonly ObjectPool<TimedMessage<T>> _pool;

    /// <summary>
    /// 静态构造函数（初始化对象池）
    /// </summary>
    static TimedMessagePool()
    {
        // 从环境变量读取池大小，默认1000
        // 保证池大小至少为100（可根据业务调整），避免负数/0
        var poolSize = int.TryParse(Environment.GetEnvironmentVariable("TIMED_MESSAGE_POOL_SIZE"), out var size)
            ? Math.Max(size, 100) // 限制最小值
            : 1000; // 默认值
        var policy = new TimedMessagePooledPolicy<T>();
        _pool = new DefaultObjectPool<TimedMessage<T>>(policy, poolSize);
    }

    /// <summary>
    /// 从池获取空对象
    /// </summary>
    /// <returns>池化的TimedMessage对象</returns>
    public static TimedMessage<T> Get()
    {
        return _pool.Get();
    }

    /// <summary>
    /// 从池获取对象并初始化核心字段
    /// </summary>
    /// <param name="payload">消息体</param>
    /// <param name="traceId">追踪ID（自动生成如果为null）</param>
    /// <param name="messageId">消息ID（自动生成如果为null）</param>
    /// <returns>初始化后的TimedMessage对象</returns>
    public static TimedMessage<T> Get(T payload, string traceId, string messageId)
    {
        var message = _pool.Get();
        message.Payload = payload;
        message.EnqueueTimeUtc = DateTime.UtcNow;
        message.TraceId = traceId ?? TraceIdGenerator.Generate();
        message.MessageId = messageId ?? $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        return message;
    }

    /// <summary>
    /// 归还对象到池
    /// </summary>
    /// <param name="obj">待归还的对象</param>
    public static void Return(TimedMessage<T> obj)
    {
        if (obj != null)
        {
            _pool.Return(obj);
        }
    }

    /// <summary>
    /// 获取复用的字节数组（用于消息序列化）
    /// </summary>
    /// <param name="length">数组长度</param>
    /// <returns>ArrayPool中的字节数组</returns>
    public static byte[] GetBinaryData(int length)
    {
        return ArrayPool<byte>.Shared.Rent(length);
    }
}
