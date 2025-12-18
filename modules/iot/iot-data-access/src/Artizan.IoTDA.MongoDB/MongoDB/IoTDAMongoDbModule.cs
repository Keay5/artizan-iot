using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;

namespace Artizan.IoTDA.MongoDB;

[DependsOn(
    typeof(IoTDADomainModule),
    typeof(AbpMongoDbModule)
    )]
public class IoTDAMongoDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMongoDbContext<IoTDAMongoDbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTDAMongoDbContext>();
            
            /* Add custom repositories here. Example:
             * options.AddRepository<Question, MongoQuestionRepository>();
             */
        });
    }
}
