
using Microsoft.Extensions.ObjectPool; 

namespace Artizan.IoT.BatchProcessing.Core;

/// <summary>
/// 消息对象池化策略
/// 【设计思路】：实现ObjectPool的池化策略，控制对象的创建和归还
/// 【设计考量】：
/// 1. Create：创建新对象
/// 2. Return：重置对象并验证有效性
/// 【设计模式】：对象池模式（Object Pool Pattern）
/// </summary>
/// <typeparam name="T">消息体类型</typeparam>
public class TimedMessagePooledPolicy<T> : IPooledObjectPolicy<TimedMessage<T>>
{
    /// <summary>
    /// 创建新的消息对象
    /// </summary>
    /// <returns>新的TimedMessage对象</returns>
    public TimedMessage<T> Create()
    {
        return new TimedMessage<T>();
    }

    /// <summary>
    /// 归还对象到池
    /// </summary>
    /// <param name="obj">待归还的对象</param>
    /// <returns>是否归还成功</returns>
    public bool Return(TimedMessage<T> obj)
    {
        if (obj == null)
        {
            return false;
        }

        obj.Reset();
        return true;
    }
}
