using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// OTA升级进度上报请求（设备→云端）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/ota-update
/// Method：thing.event.ota.upgrade.progress.post
/// 【说明】：设备在OTA升级过程中，向云端实时上报升级进度（如下载进度、安装进度）
/// </summary>
public class OtaUpgradeProgressPostRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.event.ota.upgrade.progress.post";

    [JsonPropertyName("params")]
    public OtaUpgradeProgressPostParams Params { get; set; } = new();

    /// <summary>
    /// 生成符合阿里云OTA协议规范的进度上报Topic
    /// 【Topic格式】：/sys/${productKey}/${deviceName}/thing/event/ota/upgrade/progress/post
    /// </summary>
    /// <param name="productKey">产品标识（阿里云IoT平台分配，必填）</param>
    /// <param name="deviceName">设备标识（产品下唯一，必填）</param>
    /// <returns>完整的OTA升级进度上报Topic</returns>
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
        return $"/sys/{productKey}/{deviceName}/thing/event/ota/upgrade/progress/post";
    }
}

/// <summary>
/// OTA升级进度上报参数
/// 【协议约束】：参数字段需严格匹配阿里云OTA进度上报协议，核心传递进度和状态信息
/// </summary>
public class OtaUpgradeProgressPostParams
{
    /// <summary>
    /// 升级ID（与OTA升级请求的upgradeId一致，用于关联本次升级流程，必填）
    /// </summary>
    [JsonPropertyName("upgradeId")]
    public string UpgradeId { get; set; } = string.Empty;

    /// <summary>
    /// 升级进度（百分比，范围0-100，必填）
    /// 【说明】：0表示未开始，100表示完成，中间值对应不同阶段进度（如下载30%、安装80%）
    /// </summary>
    [JsonPropertyName("progress")]
    public int Progress { get; set; } = 0;

    /// <summary>
    /// 升级状态（可选值：upgrading-升级中、success-升级成功、failed-升级失败，必填）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "upgrading";

    /// <summary>
    /// 状态描述（补充说明当前进度/状态的详细信息，如“固件下载中”“安装失败：文件损坏”）
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 剩余时间（可选，单位：秒，预估完成剩余升级所需时间）
    /// </summary>
    [JsonPropertyName("remainingTime")]
    public int? RemainingTime { get; set; }
}