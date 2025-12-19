using Artizan.IoTHub.Devices.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;

namespace Artizan.IoTHub.Devices;

[Area(IoTHubRemoteServiceConsts.ModuleName)]
[RemoteService(Name = IoTHubRemoteServiceConsts.RemoteServiceName)]
[Route("api/iot-hub/devices")]
public class DeviceController : IoTHubControllerBase, IDeviceAppService
{
    protected IDeviceAppService DeivceAppService;

    public DeviceController(IDeviceAppService deviceAdminAppService)
    {
        DeivceAppService = deviceAdminAppService;
    }

    [HttpGet]
    [Route("{id}")]
    public virtual async Task<DeviceDto> GetAsync(Guid id)
    {
        return await DeivceAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual async Task<DeviceDto> CreateAsync(CreateDeviceInput input)
    {
        return await DeivceAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual async Task<DeviceDto> UpdateAsync(Guid id, UpdateDeviceInput input)
    {
        return await DeivceAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual async Task DeleteAsync(Guid id)
    {
        await DeivceAppService.DeleteAsync(id);
    }
}
