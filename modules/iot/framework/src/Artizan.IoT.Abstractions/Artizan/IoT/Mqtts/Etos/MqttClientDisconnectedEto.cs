using System;

namespace Artizan.IoT.Mqtts.Etos;

/// <summary>
/// MQTT客户端下线事件
/// </summary>
[Serializable]
public class MqttClientDisconnectedEto : MqttEventBase
{
    public string? ProductKey { get; set; }
    public string? DeviceName { get; set; }
    public string? Endpoint { get; set; }
}
