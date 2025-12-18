using Artizan.IoT.Mqtts.Messages;
using MQTTnet;
using System;

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

        // 仅提取原Segment用于拷贝，无任何对象引用
        var originalSegment = mqttMsg.PayloadSegment;

        // 创建RawMessage（内部完成深拷贝，彻底解耦）
        return new MqttRawMessage(
            topic: mqttMsg.Topic,
            qosLevel: mqttMsg.QualityOfServiceLevel,
            retain: mqttMsg.Retain,
            originalPayloadSegment: originalSegment);
    }
}
