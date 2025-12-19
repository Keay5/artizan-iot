using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// 设备身份标识（ProductKey + DeviceName）
/// </summary>
public class DeviceIdentity
{
    /// <summary>
    /// 产品Key（阿里云平台分配）
    /// </summary>
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（产品内唯一）
    /// </summary>
    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;
}
