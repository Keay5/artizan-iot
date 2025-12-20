using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// OTA升级进度上报响应（云端→设备）
/// 【说明】：云端接收设备的升级进度上报后，向设备返回确认响应
/// </summary>
public class OtaUpgradeProgressPostResponse : AlinkResponseBase<OtaUpgradeProgressPostResponseData>
{
}

/// <summary>
/// OTA升级进度上报响应数据
/// </summary>
public class OtaUpgradeProgressPostResponseData
{
    /// <summary>
    /// 处理结果（true：接收成功；false：接收失败）
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// 响应描述（如接收成功提示、失败原因）
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "progress received successfully";
}
