using Artizan.IoTHub.Devices.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Artizan.IoTHub.Devices;

public interface IDeviceAppService : IApplicationService
{
    Task<DeviceDto> GetAsync(Guid id);

    Task<DeviceDto> CreateAsync(CreateDeviceInput input);

    Task<DeviceDto> UpdateAsync(Guid id, UpdateDeviceInput input);

    Task DeleteAsync(Guid id);
}
