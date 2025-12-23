using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备标签参数
/// </summary>
public class DeviceTagParams
{
    [JsonPropertyName("tags")]
    public List<DeviceTagItem> Tags { get; set; } = new();

    [JsonPropertyName("replace")]
    public bool Replace { get; set; } = true; // 是否替换原有标签
}

/// <summary>
/// 设备标签项
/// </summary>
public class DeviceTagItem
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
