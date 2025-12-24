using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;

namespace Artizan.IoTHub.MongoDB;

[DependsOn(
    typeof(IoTHubDomainModule),
    typeof(AbpMongoDbModule)
    )]
public class IoTHubMongoDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMongoDbContext<IoTHubMongoDbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTHubMongoDbContext>();
            
            /* Add custom repositories here. Example:
             * options.AddRepository<Question, MongoQuestionRepository>();
             */
        });
    }
}
