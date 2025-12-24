using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// Alink协议扩展配置（sys字段）
/// 【协议场景】：控制响应行为、扩展功能等
/// </summary>
public class AlinkSysConfig
{
    /// <summary>
    /// 是否需要云端响应（1=需要，0=不需要，默认1）
    /// </summary>
    [JsonPropertyName("ack")]
    public int Ack { get; set; } = 1;

    /// <summary>
    /// 超时时间（毫秒，可选）
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }
}
