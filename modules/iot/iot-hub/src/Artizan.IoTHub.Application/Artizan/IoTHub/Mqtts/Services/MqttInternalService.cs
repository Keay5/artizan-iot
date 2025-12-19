using Artizan.IoTHub.Mqtts.Servers;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Artizan.IoTHub.Mqtts.Services;

[ExposeServices(typeof(IMqttInternalService), typeof(IMqttService))]
public class MqttInternalService : MqttServiceBase, IMqttInternalService, ISingletonDependency
{
    protected ILogger<MqttInternalService> Logger { get; }
    protected IDistributedEventBus DistributedEventBus { get; }

    public MqttInternalService(
        ILogger<MqttInternalService> logger,
        IDistributedEventBus distributedEventBus)
        : base()
    {
        Logger = logger;
        DistributedEventBus = distributedEventBus;
    }

    public override void ConfigureMqttServer(MqttServer mqttServer)
    {
        base.ConfigureMqttServer(mqttServer);
    }
}
