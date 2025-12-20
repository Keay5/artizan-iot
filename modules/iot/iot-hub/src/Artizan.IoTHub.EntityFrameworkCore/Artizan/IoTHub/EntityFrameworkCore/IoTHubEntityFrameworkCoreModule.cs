using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Products;
using Artizan.IoTHub.Products.Modules;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub.EntityFrameworkCore;

[DependsOn(
    typeof(IoTHubDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class IoTHubEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<IoTHubDbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTHubDbContext>(includeAllEntities: true);

            /* Add custom repositories here. Example:
            * options.AddRepository<Question, EfCoreQuestionRepository>();
            */
            options.AddRepository<Product, EfCoreProductRepository>();
            options.AddRepository<ProductModule, EfCoreProductModuleRepository>();
            options.AddRepository<Device, EfCoreDeviceRepository>();
        });
    }
}
