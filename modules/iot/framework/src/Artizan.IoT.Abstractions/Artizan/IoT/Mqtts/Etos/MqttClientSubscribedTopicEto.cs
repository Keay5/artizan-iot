using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.EventBus;

namespace Artizan.IoT.Mqtts.Etos;

/// <summary>
/// MQTT客户端订阅主题事件
/// </summary>
[Serializable]
public class MqttClientSubscribedTopicEto : MqttEventBase
{
    public string MqttTopic { get; set; }
    public string? ProductKey { get; set; }
    public string? DeviceName { get; set; }

}
