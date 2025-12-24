using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程配置上报请求（设备→云端，上报当前生效配置）
/// Method：thing.event.config.post
/// Topic模板：/sys/${productKey}/${deviceName}/thing/event/config/post
/// </summary>
public class RemoteConfigReportRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.event.config.post";

    [JsonPropertyName("params")]
    public RemoteConfigReportParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/event/config/post";
    }
}

/// <summary>
/// 远程配置上报参数
/// </summary>
public class RemoteConfigReportParams
{
    /// <summary>
    /// 当前生效的配置版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 当前配置键值对
    /// </summary>
    [JsonPropertyName("configs")]
    public Dictionary<string, string> Configs { get; set; } = new();

    /// <summary>
    /// 配置应用状态（applied=已应用/failed=应用失败）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "applied";

    /// <summary>
    /// 上报时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
