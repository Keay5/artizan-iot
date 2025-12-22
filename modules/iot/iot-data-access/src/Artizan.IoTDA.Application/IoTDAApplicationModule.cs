using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDADomainModule),
    typeof(IoTDAApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)
    )]
public class IoTDAApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<IoTDAApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<IoTDAApplicationModule>(validate: true);
        });
    }
}
