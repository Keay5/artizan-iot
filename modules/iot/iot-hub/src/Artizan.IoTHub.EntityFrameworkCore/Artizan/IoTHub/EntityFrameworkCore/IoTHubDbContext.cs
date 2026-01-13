using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.EntityFrameworkCore;

[ConnectionStringName(IoTHubDbProperties.ConnectionStringName)]
public class IoTHubDbContext : AbpDbContext<IoTHubDbContext>, IIoTHubDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * public DbSet<Question> Questions { get; set; }
     */

    public IoTHubDbContext(DbContextOptions<IoTHubDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureIoTHub();
    }
}
