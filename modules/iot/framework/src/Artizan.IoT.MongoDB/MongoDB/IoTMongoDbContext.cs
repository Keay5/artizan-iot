using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace Artizan.IoT.MongoDB;

[ConnectionStringName(IoTDbProperties.ConnectionStringName)]
public class IoTMongoDbContext : AbpMongoDbContext, IIoTMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */

    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);

        modelBuilder.ConfigureIoT();
    }
}
