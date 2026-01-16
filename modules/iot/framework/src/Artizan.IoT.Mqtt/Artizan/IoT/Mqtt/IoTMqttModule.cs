using Artizan.IoT.Localization;
using Artizan.IoT.Mqtt.Localization;
using Artizan.IoT.Mqtt.MessageHanlders;
using Artizan.IoT.Mqtt.Messages.Dispatchers;
using Artizan.IoT.Mqtt.Options;
using Artizan.IoT.Mqtt.Topics.Routes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT.Mqtt;

[DependsOn(
    typeof(IoTCoreModule),
    typeof(IoTAbstractionsModule)
)]
public class IoTMqttModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTCoreModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<IoTMqttResource>("en")
                .AddBaseTypes(typeof(IoTResource))
                .AddVirtualJson("Artizan/IoT/Mqtt/Localization/Resources");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTMqttErrorCodes.Namespace, typeof(IoTMqttResource));
        });

        ConfigMqtt(context);
    }

    private void ConfigMqtt(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        #region MQTT Server

        services.ConfigMqttServerOptions(configuration);

        #endregion

        #region MQTT Topic Router

        // 注册MQTT主题路由选项配置
        services.ConfigMqttRouterOptions(configuration);
        // 注册MQTT主题路由缓存清理后台服务
        context.Services.AddHostedService<MqttTopicRouteCacheCleanupBackgroundService>();

        #endregion

        #region MQTT Message 分发器

        services.ConfigMqttMessageDispatcherOptions(configuration);

        #endregion
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        InitMqttTopicRouter(context);
        await StartMqttMessageDispatcherAsync(context.ServiceProvider);
    }

    public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        await StopMqttMessageDispatcherAsync(context.ServiceProvider);
    }

    #region MQTT 消息路由
    private void InitMqttTopicRouter(ApplicationInitializationContext context)
    {
        // 扫描程序集并注册MQTT Topic 路由
        MqttTopicMessageHandlerRegister.ScanAssembliesAndRegisterMqttTopicMessageHandlers(context.ServiceProvider);
    }
    #endregion

    #region MQTT 消息分发器

    private async Task StartMqttMessageDispatcherAsync(IServiceProvider serviceProvider)
    {
        var dispatcher = serviceProvider.GetRequiredService<IMqttMessageDispatcher>();
        await dispatcher.StartAsync();
    }

    private async Task StopMqttMessageDispatcherAsync(IServiceProvider serviceProvider)
    {
        var dispatcher = serviceProvider.GetRequiredService<IMqttMessageDispatcher>();
        await dispatcher.StopAsync();
    }

    #endregion
}