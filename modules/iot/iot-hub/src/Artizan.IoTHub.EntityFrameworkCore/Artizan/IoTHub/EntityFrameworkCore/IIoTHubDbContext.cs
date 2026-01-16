using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.EntityFrameworkCore;

[ConnectionStringName(IoTHubDbProperties.ConnectionStringName)]
public interface IIoTHubDbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */
}
