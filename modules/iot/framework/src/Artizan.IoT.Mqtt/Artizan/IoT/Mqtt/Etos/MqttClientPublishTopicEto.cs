using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.EventBus;

namespace Artizan.IoT.Mqtt.Etos;

/// <summary>
/// MQTT客户端发布主题事件
/// </summary>
[Serializable]
public class MqttClientPublishTopicEto : MqttEventBase
{
    public string MqttTopic { get; set; }
    public byte[]? MqttPayload { get; set; }
    public string? ProductKey { get; set; }
    public string? DeviceName { get; set; }
    public DateTime Timestamp { get; set; }
}
