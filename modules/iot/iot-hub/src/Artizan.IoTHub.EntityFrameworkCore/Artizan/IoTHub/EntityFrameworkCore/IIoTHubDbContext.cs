using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Products;
using Artizan.IoTHub.Products.Modules;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.EntityFrameworkCore;

[ConnectionStringName(IoTHubDbProperties.ConnectionStringName)]
public interface IIoTHubDbContext : IEfCoreDbContext
{
    /* Add DbSet for each Aggregate Root here. Example:
     * DbSet<Question> Questions { get; }
     */

    DbSet<Product> Products { get; }

    DbSet<ProductModule> ProductModules { get; }

    DbSet<Device> Devices { get; }

}
