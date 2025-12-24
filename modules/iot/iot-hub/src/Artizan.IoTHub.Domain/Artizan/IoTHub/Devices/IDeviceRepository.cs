using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Volo.Abp.Domain.Repositories;

namespace Artizan.IoTHub.Devices
{
    public interface IDeviceRepository : IRepository<Device, Guid>
    {
        Task<Device?> FindByDeviceNameAsync(Guid productId, string deviceName, CancellationToken cancellationToken = default);
        Task<Device> GetByDeviceNameAsync(Guid productId, string deviceName, CancellationToken cancellationToken = default);
        Task<List<Device>> GetDevicesByProductId(Guid productId, CancellationToken cancellationToken = default);
    }
}
