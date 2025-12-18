using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace Artizan.IoT.MongoDB;

[ConnectionStringName(IoTDbProperties.ConnectionStringName)]
public interface IIoTMongoDbContext : IAbpMongoDbContext
{
    /* Define mongo collections here. Example:
     * IMongoCollection<Question> Questions { get; }
     */
}
