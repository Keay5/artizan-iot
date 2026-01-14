namespace Artizan.IoT.Mqtt.Options;

/// <summary>
/// MQTT服务器配置选项
/// </summary>
public class MqttServerOptions
{
    /// <summary>
    /// 服务器IP地址（无默认值，建议配置）
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// 服务器域名（无默认值，与IpAddress二选一配置）
    /// </summary>
    public string DomainName { get; set; }

    /// <summary>
    /// MQTT默认端口（默认1883，行业标准）
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 是否启用TLS加密（默认false）
    /// </summary>
    public bool EnableTls { get; set; } = false;

    /// <summary>
    /// MQTT TLS加密端口（默认8883，行业标准）
    /// </summary>
    public int TlsPort { get; set; } = 8883;

    /// <summary>
    /// MQTT WebSocket端口（默认5883）
    /// </summary>
    public int WebSocketPort { get; set; } = 5883;
}
