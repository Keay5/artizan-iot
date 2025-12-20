using System;

namespace Artizan.IoT.Mqtts.Topics.Permissions;

/// <summary>
/// MQTT Topic权限校验上下文（封装校验所需所有信息）
/// </summary>
public class MqttTopicPermissionContext
{
    /// <summary>
    /// 追踪ID（日志关联）
    /// </summary>
    public string TrackId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 认证上下文的产品Key（设备认证后的值）
    /// </summary>
    public string AuthProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 认证上下文的设备名称（设备认证后的值）
    /// </summary>
    public string AuthDeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 待校验的Topic
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型（发布/订阅）
    /// </summary>
    public MqttTopicOperation Operation { get; set; }

    /// <summary>
    /// 从Topic解析出的产品Key（路由层解析后的值）
    /// </summary>
    public string? ParsedProductKey { get; set; }

    /// <summary>
    /// 从Topic解析出的设备名称（路由层解析后的值）
    /// </summary>
    public string? ParsedDeviceName { get; set; }
}
