using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 设备登出响应
/// </summary>
public class DeviceLogoutResponse : AlinkResponseBase<DeviceLogoutResponseData>
{
}

/// <summary>
/// 登出响应数据
/// </summary>
public class DeviceLogoutResponseData
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("logoutTime")]
    public long LogoutTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
