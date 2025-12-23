using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程配置设置响应（设备→云端）
/// </summary>
public class RemoteConfigSetResponse : AlinkResponseBase<RemoteConfigSetResponseData>
{
}

/// <summary>
/// 远程配置设置响应数据
/// </summary>
public class RemoteConfigSetResponseData
{
    /// <summary>
    /// 是否成功应用配置
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// 当前生效的配置版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 配置应用时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("applyTime")]
    public long ApplyTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 应用失败原因（成功时为空）
    /// </summary>
    [JsonPropertyName("failReason")]
    public string FailReason { get; set; } = string.Empty;
}
