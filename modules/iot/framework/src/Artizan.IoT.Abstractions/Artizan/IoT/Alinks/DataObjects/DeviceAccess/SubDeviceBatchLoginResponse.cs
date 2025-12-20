using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 子设备批量登录响应
/// </summary>
public class SubDeviceBatchLoginResponse : AlinkResponseBase<List<SubDeviceLoginResult>>
{
}

/// <summary>
/// 子设备登录结果
/// </summary>
public class SubDeviceLoginResult
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; } = 200;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "success";
}
