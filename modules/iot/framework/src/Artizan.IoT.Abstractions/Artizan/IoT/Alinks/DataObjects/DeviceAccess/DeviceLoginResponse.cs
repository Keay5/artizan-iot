using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 设备登录响应
/// </summary>
public class DeviceLoginResponse : AlinkResponseBase<DeviceLoginResponseData>
{
}

/// <summary>
/// 设备登录响应数据
/// </summary>
public class DeviceLoginResponseData
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("iotId")]
    public string IotId { get; set; } = string.Empty;

    [JsonPropertyName("expireTime")]
    public long ExpireTime { get; set; }
}
