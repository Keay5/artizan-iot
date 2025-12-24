using Artizan.IoT.Mqtts.Messages;
using System;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.MessageHanlders;

/// <summary>
/// 线程安全的MQTT消息处理器基类（推荐业务Handler继承）
/// 设计理念：
/// 1. 内置同步锁：简化有状态Handler的线程安全实现，避免手动加锁；
/// 2. 抽象处理方法：强制子类实现业务逻辑，保障规范一致；
/// 3. 降低门槛：业务开发无需关注线程安全细节，专注业务逻辑。
/// 
/// 注意：抽象虚基类：提供通用逻辑，不实例化、不注册、不标记路由特性
/// </summary>
public abstract class SafeMqttMessageHandler : IMqttMessageHandler, IDisposable
{
    /// <summary>
    /// 内置同步锁对象（用于保护共享状态）
    /// </summary>
    protected readonly object SyncRoot = new();

    public virtual void Dispose()
    {
        // 子类若持有非托管资源，可重写此方法释放
    }

    public abstract Task HandleAsync(MqttMessageContext context);
}
