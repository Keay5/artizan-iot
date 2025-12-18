using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace Artizan.IoTHub.MongoDB;

[ConnectionStringName(IoTHubDbProperties.ConnectionStringName)]
public interface IIoTHubMongoDbContext : IAbpMongoDbContext
{
    /* Define mongo collections here. Example:
     * IMongoCollection<Question> Questions { get; }
     */
}
