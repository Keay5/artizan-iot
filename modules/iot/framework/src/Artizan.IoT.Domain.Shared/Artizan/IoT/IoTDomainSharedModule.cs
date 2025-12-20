using Artizan.IoT.Localization;
using Volo.Abp.Domain;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT;

[DependsOn(
    typeof(AbpValidationModule),
    typeof(IoTCoreModule),
    typeof(AbpDddDomainSharedModule)
)]
public class IoTDomainSharedModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTDomainSharedModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTResource>()
                .AddVirtualJson("Artizan/IoT/Localization/DomainShared");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTErrorCodes.Namespace, typeof(IoTResource));
        });
    }
}
