using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// 子设备批量数据
/// </summary>
public class GatewayBatchPostSubDeviceData
{
    /// <summary>
    /// 子设备身份
    /// </summary>
    [JsonPropertyName("identity")]
    public DeviceIdentity Identity { get; set; } = new();

    /// <summary>
    /// 子设备属性集合
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, GatewayBatchPostItem> Properties { get; set; } = new();

    /// <summary>
    /// 子设备事件集合
    /// </summary>
    [JsonPropertyName("events")]
    public Dictionary<string, GatewayBatchPostItem> Events { get; set; } = new();
}