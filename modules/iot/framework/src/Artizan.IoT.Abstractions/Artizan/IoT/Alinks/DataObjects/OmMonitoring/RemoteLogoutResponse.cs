using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程登出响应（设备→云端）
/// </summary>
public class RemoteLogoutResponse : AlinkResponseBase<RemoteLogoutResponseData>
{
}

/// <summary>
/// 远程登出响应数据
/// </summary>
public class RemoteLogoutResponseData
{
    /// <summary>
    /// 远程登录会话ID
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 登出结果（success/failed）
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = "success";

    /// <summary>
    /// 登出时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("logoutTime")]
    public long LogoutTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 会话总持续时长（秒）
    /// </summary>
    [JsonPropertyName("totalDuration")]
    public int TotalDuration { get; set; } = 0;
}

