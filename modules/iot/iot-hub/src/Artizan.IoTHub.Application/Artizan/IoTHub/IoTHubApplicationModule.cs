using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Dispatchers;
using Artizan.IoT.Mqtts.Topics.Routes;
using Artizan.IoTHub.Mqtts.Messages.PostProcessors.Caches;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubDomainModule),
    typeof(IoTHubApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)
 )]
public class IoTHubApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<IoTHubApplicationModule>();
        var configuration = context.Services.GetConfiguration();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<IoTHubApplicationModule>(validate: true);
        });

        ConfigureMqttServices(context);
    }

    private static void ConfigureMqttServices(ServiceConfigurationContext context)
    {
        #region MQTT 消息缓存
        //// 关键：注册为IHostedService（让ABP托管其生命周期，自动启动/停止）
        //// 因为该类实现了IHostedService，必须通过AddHostedService注册才能被主机发现
        //context.Services.AddHostedService<MqttCacheMessagePostProcessor_Backup2>();

        #endregion
    }

    //public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    //{
    //    await StartMqttMessageCachePostProcessorAsync(context);
    //}


    //public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    //{
    //    await StopMqttMessageCachePostProcessorAsync(context);
    //}

    //#region MQTT 消息缓存
    //private async Task StartMqttMessageCachePostProcessorAsync(ApplicationInitializationContext context)
    //{
    //    var mqttMessageCachePostProcessor = context.ServiceProvider.GetRequiredService<IMqttCacheMessagePostProcessor<MqttMessageContext>>() ;
    //    await mqttMessageCachePostProcessor.StartAsync();
    //}

    //private async Task StopMqttMessageCachePostProcessorAsync(ApplicationShutdownContext context)
    //{
    //    var mqttMessageCachePostProcessor = context.ServiceProvider.GetRequiredService<IMqttCacheMessagePostProcessor<MqttMessageContext>>();
    //    await mqttMessageCachePostProcessor.StopAsync();
    //} 
    //#endregion
}

