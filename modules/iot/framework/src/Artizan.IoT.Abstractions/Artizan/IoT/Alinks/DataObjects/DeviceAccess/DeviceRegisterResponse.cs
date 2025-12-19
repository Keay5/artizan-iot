using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 设备注册响应
/// </summary>
public class DeviceRegisterResponse : AlinkResponseBase<DeviceRegisterResponseData>
{
}

/// <summary>
/// 设备注册响应数据
/// </summary>
public class DeviceRegisterResponseData
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("iotId")]
    public string IotId { get; set; } = string.Empty;

    [JsonPropertyName("registerTime")]
    public long RegisterTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}