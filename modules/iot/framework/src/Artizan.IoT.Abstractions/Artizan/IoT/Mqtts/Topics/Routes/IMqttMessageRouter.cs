using Artizan.IoT.Mqtts.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Topics.Routes;

/// <summary>
/// MQTT消息路由接口
/// 【设计理念】：接口隔离原则 - 仅暴露必要的路由方法，避免接口膨胀
/// </summary>
public interface IMqttMessageRouter
{
    /// <summary>
    /// 路由消息到匹配的Handler(Handler进行后续处理)
    /// </summary>
    Task RouteMessageAsync(MqttMessageContext context, CancellationToken cancellationToken = default);
}
