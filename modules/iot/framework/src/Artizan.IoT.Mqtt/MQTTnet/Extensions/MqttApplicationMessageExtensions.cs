using Artizan.IoT.Mqtt.Messages;
using System;
using System.Buffers;
using System.Linq;

namespace MQTTnet.Extensions;

/// <summary>
/// MQTT消息扩展方法（转换为彻底解耦的MqttRawMessage）
/// </summary>
public static class MqttApplicationMessageExtensions
{
    /// <summary>
    /// 将MqttApplicationMessage转换为MqttRawMessage（初始化即深拷贝，彻底断绝原数组引用）
    /// </summary>
    public static MqttRawMessage ToMqttRawMessage(this MqttApplicationMessage mqttMsg)
    {
        if (mqttMsg == null)
        {
            throw new ArgumentNullException(nameof(mqttMsg), "原MQTT消息不能为空");
        }

        // 创建RawMessage
        return new MqttRawMessage(
            topic: mqttMsg.Topic,
            qosLevel: mqttMsg.QualityOfServiceLevel,
            retain: mqttMsg.Retain,
            payloadSegment: new ArraySegment<byte>(mqttMsg.Payload.ToArray()));
    }
}
