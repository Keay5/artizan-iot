using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Artizan.IoT.EntityFrameworkCore;

[DependsOn(
    typeof(IoTDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class IoTEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<IoTDbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTDbContext>(includeAllEntities: true);
            
            /* Add custom repositories here. Example:
            * options.AddRepository<Question, EfCoreQuestionRepository>();
            */
        });
    }
}
