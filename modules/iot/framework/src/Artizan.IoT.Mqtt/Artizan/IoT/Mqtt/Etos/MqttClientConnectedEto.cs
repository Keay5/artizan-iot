using MQTTnet.Formatter;
using System;

namespace Artizan.IoT.Mqtt.Etos;

/// <summary>
/// MQTT客户端上线事件
/// </summary>
//[EventName(MqttServerEventConsts.ClientConnected)] // EventName 属性是可选的，如果没有为事件类型(ETO 类)声明它，则事件名称将是事件类的全名.
[Serializable]
public class MqttClientConnectedEto : MqttEventBase
{
    /// <summary>
    ///     The protocol version which is used by the connected client.
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; set; }

    /// <summary>
    ///  the endpoint of the connected client.
    /// </summary>
    public string Endpoint { get; set; }
    public string? ProductKey { get; set; }
    public string? DeviceName { get; set; }
}
