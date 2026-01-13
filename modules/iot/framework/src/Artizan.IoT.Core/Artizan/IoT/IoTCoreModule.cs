using Artizan.IoT.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT;

[DependsOn(
    typeof(AbpValidationModule)
)]
public class IoTCoreModule : AbpModule
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
                .Add<IoTResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("Artizan/IoT/Localization/Resources/Core");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTErrorCodes.Namespace, typeof(IoTResource));
        });
    }

}