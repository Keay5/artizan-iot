using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTDA.EntityFrameworkCore;

[ConnectionStringName(IoTDADbProperties.ConnectionStringName)]
public class IoTDADbContext : AbpDbContext<IoTDADbContext>, IIoTDADbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * public DbSet<Question> Questions { get; set; }
     */

    public IoTDADbContext(DbContextOptions<IoTDADbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureIoTDA();
    }
}
