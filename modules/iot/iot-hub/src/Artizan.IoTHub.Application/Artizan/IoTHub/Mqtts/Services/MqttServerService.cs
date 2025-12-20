using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using System;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Mqtts.Services;

public class MqttServerService : IMqttServerService, ISingletonDependency
{
    protected IServiceProvider ServiceProvider { get; }

    public MqttServer MqttServer { get; private set; }

    public MqttServerService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void ConfigureMqttService(MqttServer mqttServer)
    {
        MqttServer = mqttServer;

        var mqttServices = ServiceProvider.GetServices<IMqttService>();
        foreach (var mqttService in mqttServices)
        {
            mqttService.ConfigureMqttServer(mqttServer);
        }
    }
}
