using Localization.Resources.AbpUi;
using Artizan.IoT.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class IoTHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(IoTHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
