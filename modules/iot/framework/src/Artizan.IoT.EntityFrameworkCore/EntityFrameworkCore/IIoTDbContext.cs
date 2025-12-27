using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoT.EntityFrameworkCore;

[ConnectionStringName(IoTDbProperties.ConnectionStringName)]
public interface IIoTDbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */
}
