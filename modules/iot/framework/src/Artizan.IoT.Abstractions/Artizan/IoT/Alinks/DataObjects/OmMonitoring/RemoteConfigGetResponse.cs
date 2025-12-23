using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程配置获取响应
/// </summary>
public class RemoteConfigGetResponse : AlinkResponseBase<RemoteConfigData>
{
}

/// <summary>
/// 远程配置数据
/// </summary>
public class RemoteConfigData
{
    /// <summary>
    /// 配置版本（建议用时间戳或语义化版本）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 配置键值对
    /// </summary>
    [JsonPropertyName("configs")]
    public Dictionary<string, string> Configs { get; set; } = new();

    /// <summary>
    /// 配置下发时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 配置有效期（秒，0表示永久有效）
    /// </summary>
    [JsonPropertyName("expireSeconds")]
    public int ExpireSeconds { get; set; } = 0;
}