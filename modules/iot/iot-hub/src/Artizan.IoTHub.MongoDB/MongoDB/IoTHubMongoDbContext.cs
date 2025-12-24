using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace Artizan.IoTHub.MongoDB;

[ConnectionStringName(IoTHubDbProperties.ConnectionStringName)]
public class IoTHubMongoDbContext : AbpMongoDbContext, IIoTHubMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */

    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);

        modelBuilder.ConfigureIoTHub();
    }
}
