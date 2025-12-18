using Artizan.IoTHub.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.Devices
{
    public class EfCoreDeviceRepository : 
        EfCoreRepository<IIoTHubDbContext, Device, Guid>,
        IDeviceRepository
    {
        public EfCoreDeviceRepository(IDbContextProvider<IIoTHubDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        public virtual async Task<Device?> FindByDeviceNameAsync(Guid productId, string deviceName, CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync()).SingleOrDefaultAsync(p => p.ProductId == productId && p.DeviceName == deviceName, GetCancellationToken(cancellationToken));
        }

        public virtual async Task<Device> GetByDeviceNameAsync(Guid productId, string deviceName, CancellationToken cancellationToken = default)
        {
            return await GetAsync(p => p.ProductId == productId && p.DeviceName == deviceName, cancellationToken: GetCancellationToken(cancellationToken));
        }

        public virtual async Task<List<Device>> GetDevicesByProductId(Guid productId, CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.CreationTime)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }
    }
}
