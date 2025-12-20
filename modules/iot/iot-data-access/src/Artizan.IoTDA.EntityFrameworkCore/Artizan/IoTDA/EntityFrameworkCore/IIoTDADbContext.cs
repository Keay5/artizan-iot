using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTDA.EntityFrameworkCore;

[ConnectionStringName(IoTDADbProperties.ConnectionStringName)]
public interface IIoTDADbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */
}
