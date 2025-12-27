using Localization.Resources.AbpUi;
using Artizan.IoTDA.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDAApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class IoTDAHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(IoTDAHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTDAResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
