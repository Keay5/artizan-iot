using MQTTnet.Server;

namespace Artizan.IoTHub.Mqtts.Servers;

public interface IMqttServerService
{
    MqttServer MqttServer { get; }

    /// <summary>
    /// 配置MqttServer
    /// </summary>
    /// <param name="mqttServer"></param>
    void ConfigureMqttService(MqttServer mqttServer);
}
