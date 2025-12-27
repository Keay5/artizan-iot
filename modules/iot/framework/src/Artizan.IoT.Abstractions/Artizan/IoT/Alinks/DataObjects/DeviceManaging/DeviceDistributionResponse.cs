using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备分发响应
/// </summary>
public class DeviceDistributionResponse : AlinkResponseBase<DeviceDistributionResponseData>
{
}

/// <summary>
/// 设备分发响应数据
/// </summary>
public class DeviceDistributionResponseData
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("distributeTime")]
    public long DistributeTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";
}