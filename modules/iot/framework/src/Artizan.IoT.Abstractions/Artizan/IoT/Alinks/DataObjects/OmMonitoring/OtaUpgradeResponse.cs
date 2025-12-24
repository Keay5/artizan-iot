using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// OTA升级响应（设备→云端）
/// 【说明】：设备接收OTA升级指令后，向云端返回是否接受升级的响应
/// </summary>
public class OtaUpgradeResponse : AlinkResponseBase<OtaUpgradeResponseData>
{
}

/// <summary>
/// OTA升级响应数据
/// </summary>
public class OtaUpgradeResponseData
{
    /// <summary>
    /// 是否接受升级（true：接受；false：拒绝）
    /// </summary>
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; } = true;

    /// <summary>
    /// 响应描述（如接受原因、拒绝原因，默认success）
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "success";

    /// <summary>
    /// 升级ID（设备生成或复用云端传递的ID，用于唯一标识本次升级流程）
    /// </summary>
    [JsonPropertyName("upgradeId")]
    public string UpgradeId { get; set; } = string.Empty;
}
