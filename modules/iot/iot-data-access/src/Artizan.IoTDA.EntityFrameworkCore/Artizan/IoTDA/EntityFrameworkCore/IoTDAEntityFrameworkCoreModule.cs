using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Artizan.IoTDA.EntityFrameworkCore;

[DependsOn(
    typeof(IoTDADomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class IoTDAEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<IoTDADbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTDADbContext>(includeAllEntities: true);
            
            /* Add custom repositories here. Example:
            * options.AddRepository<Question, EfCoreQuestionRepository>();
            */
        });
    }
}
