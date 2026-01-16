using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace Artizan.IoTHub.MongoDB;

[DependsOn(
    typeof(IoTHubApplicationTestModule),
    typeof(IoTHubMongoDbModule)
)]
public class IoTHubMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = MongoDbFixture.GetRandomConnectionString();
        });
    }
}
