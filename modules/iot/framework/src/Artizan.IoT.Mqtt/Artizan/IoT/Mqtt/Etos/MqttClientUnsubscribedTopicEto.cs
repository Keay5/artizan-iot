using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.EventBus;

namespace Artizan.IoT.Mqtt.Etos;

/// <summary>
/// MQTT客户端取消订阅主题事件 
/// </summary>
[Serializable]
public class MqttClientUnsubscribedTopicEto : MqttEventBase
{
    public string MqttTopic { get; set; }
    public string? ProductKey { get; set; }
    public string? DeviceName { get; set; }
}
