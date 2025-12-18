using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 批量属性/事件项（带时间戳）
/// </summary>
public class GatewayBatchPostItem
{
    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
