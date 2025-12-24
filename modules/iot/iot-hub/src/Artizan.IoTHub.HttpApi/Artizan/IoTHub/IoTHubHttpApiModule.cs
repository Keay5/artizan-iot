using Localization.Resources.AbpUi;
using Artizan.IoTHub.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class IoTHubHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(IoTHubHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTHubResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
