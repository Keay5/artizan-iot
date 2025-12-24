using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 拓扑关系管理响应
/// </summary>
public class TopologyManageResponse : AlinkResponseBase<List<TopologyDeviceResult>>
{
}

/// <summary>
/// 拓扑操作结果项
/// </summary>
public class TopologyDeviceResult
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
