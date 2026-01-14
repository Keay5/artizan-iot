using Artizan.IoT.Mqtt.Messages;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.MessageHanlders;

/// <summary>
/// MQTT消息处理器接口（所有业务Handler必须实现）
/// 设计理念：
/// 1. 统一Handler入口：路由系统通过此接口调度，保障所有Handler规范一致；
/// 2. 直接接收MqttMessageContext：上下文贯穿全流程，Handler无需额外参数；
/// 3. 异步设计：适配高并发场景，支持数据库操作、远程调用等异步业务。
/// </summary>
public interface IMqttTopicMessageHandler
{
    Task HandleAsync(MqttMessageContext context);
}
