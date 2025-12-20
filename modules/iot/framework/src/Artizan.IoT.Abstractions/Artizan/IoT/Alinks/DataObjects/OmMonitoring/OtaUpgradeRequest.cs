using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// OTA升级请求（云端→设备）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/ota-update
/// Method：thing.service.ota.upgrade
/// 【说明】：用于云端向设备发起OTA升级指令，包含固件信息、签名信息等核心参数
/// </summary>
public class OtaUpgradeRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.service.ota.upgrade";

    [JsonPropertyName("params")]
    public OtaUpgradeParams Params { get; set; } = new();

    /// <summary>
    /// 生成符合阿里云OTA协议规范的Topic
    /// 【Topic格式】：/sys/${productKey}/${deviceName}/thing/service/ota/upgrade
    /// </summary>
    /// <param name="productKey">产品标识（阿里云IoT平台分配，必填）</param>
    /// <param name="deviceName">设备标识（产品下唯一，必填）</param>
    /// <returns>完整的OTA升级指令Topic</returns>
    /// <exception cref="ArgumentNullException">ProductKey或DeviceName为空时抛出</exception>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空（协议约束：Topic必须包含产品标识）");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空（协议约束：Topic必须包含设备标识）");
        }
        return $"/sys/{productKey}/{deviceName}/thing/service/ota/upgrade";
    }
}

/// <summary>
/// OTA升级参数
/// 【协议约束】：参数字段需严格匹配阿里云OTA升级协议，用于传递固件核心信息
/// </summary>
public class OtaUpgradeParams
{
    /// <summary>
    /// 固件ID（阿里云IoT平台固件管理中分配的唯一标识，必填）
    /// </summary>
    [JsonPropertyName("firmwareId")]
    public string FirmwareId { get; set; } = string.Empty;

    /// <summary>
    /// 固件版本号（需符合语义化版本规范，如V1.0.0，必填）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 固件下载链接（云端存储固件的地址，设备通过此链接下载，必填）
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 固件签名（用于设备校验固件完整性，防止被篡改，必填）
    /// </summary>
    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    /// <summary>
    /// 签名方法（默认hmacsha256，支持阿里云IoT平台认可的签名算法）
    /// </summary>
    [JsonPropertyName("signMethod")]
    public string SignMethod { get; set; } = "hmacsha256";

    /// <summary>
    /// 升级类型（firmware：固件升级；config：配置升级，默认firmware）
    /// </summary>
    [JsonPropertyName("upgradeType")]
    public string UpgradeType { get; set; } = "firmware";

    /// <summary>
    /// 升级超时时间（单位：秒，默认3600秒，超时未完成则视为升级失败）
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 3600;

    /// <summary>
    /// 是否强制升级（false：设备可拒绝；true：设备必须执行升级，默认false）
    /// </summary>
    [JsonPropertyName("force")]
    public bool Force { get; set; } = false;
}
