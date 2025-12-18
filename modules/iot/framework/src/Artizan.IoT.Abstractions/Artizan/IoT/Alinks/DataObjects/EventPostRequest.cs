using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 设备上报事件请求
/// Topic: /sys/${productKey}/${deviceName}/thing/event/${eventId}/post
/// 自定义模块: /sys/${productKey}/${deviceName}/thing/event/${模块ID}:${eventId}/post
/// </summary>
public class EventPostRequest : AlinkRequestBase
{
    [JsonPropertyName("sys")]
    public AlinkSysConfig? Sys { get; set; }

    /// <summary>
    /// 事件参数
    /// </summary>
    [JsonPropertyName("params")]
    public EventParams Params { get; set; } = new();

    /// <summary>
    /// 请求方法（格式：thing.event.${eventId}.post）
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}
