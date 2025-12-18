using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace Artizan.IoTDA.MongoDB;

[DependsOn(
    typeof(IoTDAApplicationTestModule),
    typeof(IoTDAMongoDbModule)
)]
public class IoTDAMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = MongoDbFixture.GetRandomConnectionString();
        });
    }
}
