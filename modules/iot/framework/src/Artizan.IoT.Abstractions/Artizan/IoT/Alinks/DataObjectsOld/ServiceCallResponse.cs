using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// 设备响应服务调用结果
/// Topic: /sys/${productKey}/${deviceName}/thing/service/${serviceId}_reply
/// </summary>
public class ServiceCallResponse : AlinkResponseBase
{
    /// <summary>
    /// 服务返回数据（无则为空）
    /// </summary>
    [JsonPropertyName("data")]
    public new Dictionary<string, object>? Data { get; set; }
}
