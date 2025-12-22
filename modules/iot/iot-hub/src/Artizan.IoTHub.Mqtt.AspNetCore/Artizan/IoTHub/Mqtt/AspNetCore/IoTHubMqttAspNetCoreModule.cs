using Artizan.IoT.Mqtts.Options;
using Artizan.IoTHub.Mqtt.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub.Mqtt.AspNetCore;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(IoTHubApplicationModule)
)]
public class IoTHubMqttAspNetCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        ConfigureIoTHubMqttServer(context, configuration);
    }

    private void ConfigureIoTHubMqttServer(ServiceConfigurationContext context, IConfiguration configuration)
    {
        // TODO: bug, 获取到null
        //var ioTHubMqttAspNetcoreOptions = context.Services.GetRequiredService<IoTHubMqttAspNetcoreOptions>();

        var iotMqttServerOptions = context.Configuration
                //.GetSection(ioTHubMqttAspNetcoreOptions.IoTHubMqttServerConfigName)
                .GetSection("IoTHubMqttServer")
                .Get<IoTMqttServerOptions>();

        context.Services.AddHostedMqttServerWithServices(builder =>
        {
            /*-------------------------------------------------------------------------------------------
            如果不使用 Kestrel，必须调用 builder.WithDefaultEndpoint()，否则 Mqtt client 无法连接
            */
            builder.WithDefaultEndpoint();
            /*-------------------------------------------------------------------------------------------*/
            builder.WithDefaultEndpointPort(iotMqttServerOptions!.Port);
        });

        context.Services.AddMqttConnectionHandler();
        context.Services.AddConnections();
        context.Services.AddMqttTcpServerAdapter();
        context.Services.AddMqttWebSocketServerAdapter();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        app.UseIoTHubMqttServer();
    }
}