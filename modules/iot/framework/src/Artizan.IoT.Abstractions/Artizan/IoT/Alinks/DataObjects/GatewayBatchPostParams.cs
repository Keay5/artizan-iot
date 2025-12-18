using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 网关批量上报参数
/// </summary>
public class GatewayBatchPostParams
{
    /// <summary>
    /// 网关自身属性
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, GatewayBatchPostItem> Properties { get; set; } = new();

    /// <summary>
    /// 网关自身事件
    /// </summary>
    [JsonPropertyName("events")]
    public Dictionary<string, GatewayBatchPostItem> Events { get; set; } = new();

    /// <summary>
    /// 子设备数据列表（最多20个）
    /// </summary>
    [JsonPropertyName("subDevices")]
    public List<GatewayBatchPostSubDeviceData> SubDevices { get; set; } = new();
}
