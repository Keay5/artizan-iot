using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoT.EntityFrameworkCore;

[ConnectionStringName(IoTDbProperties.ConnectionStringName)]
public class IoTDbContext : AbpDbContext<IoTDbContext>, IIoTDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * public DbSet<Question> Questions { get; set; }
     */

    public IoTDbContext(DbContextOptions<IoTDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureIoT();
    }
}
