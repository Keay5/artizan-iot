using Artizan.IoT;
using Artizan.IoT.Localization;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Mqtts.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(AbpValidationModule),
    typeof(AbpDddDomainSharedModule),
    typeof(IoTCoreModule)
)]
public class IoTHubDomainSharedModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTHubDomainSharedModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<IoTHubResource>("en")
                .AddBaseTypes(typeof(IoTResource))
                .AddVirtualJson("Artizan/IoTHub/Localization/Resources");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTHubErrorCodes.Namespace, typeof(IoTHubResource));
        });

        /*------------------------------------------------------------------------------------------------------------------------
         告诉 Options 系统：“IoTMqttOptions 这个类型，永久关联到 IConfiguration 的 IoTMqtt 节点”；
         在 DI 初始化时执行，但它不是「一次性赋值」，而是「注册关联关系」。
         Options 系统会维护 IoTMqttOptions 与 IConfiguration 节点的动态绑定关系—— 当配置文件修改导致 IConfiguration 重载时，
         Options 系统会自动重新读取 IoTMqtt 节点的值，更新 IoTMqttOptions，并触发 IOptionsMonitor<T> 的 OnChange 回调。
         */
        // 绑定IoTMqttOptions到配置文件的IoTMqtt节点
        Configure<IoTMqttOptions>(configuration.GetSection("IoTMqtt"));
    }
}
