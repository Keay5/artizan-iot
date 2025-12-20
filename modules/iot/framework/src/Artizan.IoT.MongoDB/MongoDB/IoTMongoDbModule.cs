using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;

namespace Artizan.IoT.MongoDB;

[DependsOn(
    typeof(IoTDomainModule),
    typeof(AbpMongoDbModule)
    )]
public class IoTMongoDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMongoDbContext<IoTMongoDbContext>(options =>
        {
            options.AddDefaultRepositories<IIoTMongoDbContext>();
            
            /* Add custom repositories here. Example:
             * options.AddRepository<Question, MongoQuestionRepository>();
             */
        });
    }
}
