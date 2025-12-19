using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 子设备管理响应
/// </summary>
public class SubDeviceManageResponse : AlinkResponseBase<SubDeviceManageResponseData>
{
}

/// <summary>
/// 子设备管理响应数据
/// </summary>
public class SubDeviceManageResponseData
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // disabled/enabled/deleted

    [JsonPropertyName("operateTime")]
    public long OperateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}