using System;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// 原始MQTT消息封装（用于上下文存储）
/// </summary>
public class MqttRawMessage
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset ReceiveTime { get; set; } = DateTimeOffset.UtcNow;
}
