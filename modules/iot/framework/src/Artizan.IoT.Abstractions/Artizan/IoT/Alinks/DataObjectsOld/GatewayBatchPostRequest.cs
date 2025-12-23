using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// 网关批量上报请求
/// Topic: /sys/${productKey}/${deviceName}/thing/event/property/pack/post
/// </summary>
public class GatewayBatchPostRequest
{
    [JsonPropertyName("params")]
    public GatewayBatchPostParams Params { get; set; } = new();

    [JsonPropertyName("method")]
    public string Method { get; set; } = "thing.event.property.pack.post";
}
