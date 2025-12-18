using MQTTnet.Server;

namespace Artizan.IoTHub.Mqtts.Servers;

public interface IMqttService
{
    MqttServer MqttServer { get; }
    /// <summary>
    /// 配置MqttServer
    /// </summary>
    /// <param name="mqttServer"></param>
    void ConfigureMqttServer(MqttServer mqttServer);
}
