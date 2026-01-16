namespace Artizan.IoT.Mqtt.Auth;

/// <summary>
/// MQTT 连接参数
/// </summary>
public class MqttConnectParams
{
    public string ClientId { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

