using Artizan.IoT.Localization;
using Volo.Abp.Caching;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Settings;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTCoreModule),
    typeof(AbpCachingModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(AbpSettingsModule),
    typeof(AbpThreadingModule)
)]
public class IoTAbstractionsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTAbstractionsModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTResource>()
                .AddVirtualJson("Artizan/IoT/Localization/Abstractions");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTErrorCodes.Namespace, typeof(IoTResource));
        });
    }

}