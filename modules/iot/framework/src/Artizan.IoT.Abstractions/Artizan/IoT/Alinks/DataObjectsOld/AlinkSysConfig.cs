using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// Alink请求扩展配置（控制是否需要云端响应）
/// </summary>
public class AlinkSysConfig
{
    /// <summary>
    /// 是否需要响应（1=需要，0=不需要，默认1）
    /// </summary>
    [JsonPropertyName("ack")]
    public int Ack { get; set; } = 1;
}
