using Artizan.IoT.Mqtts.Messages.Dispatchers;
using Artizan.IoT.Mqtts.Topics.Routes;
using Artizan.IoT.Things.Caches.BackgroundServices;
using Artizan.IoT.Things.Caches.Extensions;
using Artizan.IoT.Things.Caches.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.Modularity;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTAbstractionsModule)
)]
public class IoTModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureThingCacheServices(context);
    }

    private static void ConfigureThingCacheServices(ServiceConfigurationContext context)
    {
        /* ----------------------------------------------------------------------------------------------
         原则：“谁拥有功能，谁负责注册” 
         设备缓存是 IoT 模块的核心功能，因此由 Artizan.IoT Module 负责 AddThingCache 的注册，
         调用者仅作为 “配置传递者 + 模块组合者”，符合 Abp 模块化架构的核心设计思想。
         */

        // 注册缓存服务 ThingCache 相关服务
        context.Services.AddThingCache(
            configureLatestOptions: options => context.Configuration.Bind("IoT:Cache:ThingPropertyData", options),
            configureHistoryOptions: options => context.Configuration.Bind("IoT:Cache:ThingPropertyHistoryData", options)
        );
        // 注册缓存清理后台服务
        context.Services.AddHostedService<ThingCacheCleanupBackgroundService>();
    }
}

