using Artizan.IoTHub.Devices.Exceptions;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Products;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.Domain.Services;

namespace Artizan.IoTHub.Devices;

public class DeviceManager : DomainService
{
    protected IDeviceRepository DeviceRepository { get; }
    protected IProductRepository ProductRepository { get; }
    protected IDeviceSecretGenerator DeviceSecretGenerator { get; }
    protected IStringLocalizer<IoTHubResource> Localizer { get; }

    public DeviceManager(
        IDeviceRepository deviceRepository,
        IProductRepository productRepository,
        IDeviceSecretGenerator deviceSecretGenerator,
        IStringLocalizer<IoTHubResource> localizer)
    {
        DeviceRepository = deviceRepository;
        ProductRepository = productRepository;
        DeviceSecretGenerator = deviceSecretGenerator;
        Localizer = localizer;
    }

    public virtual async Task<Device> CreateDeviceAsync(
        Guid productId,
        string deviceName,
        string? remarkName,
        string? description = null)
    {
        await CheckDeviceNameDuplication(productId, deviceName);

        var device = new Device(
            id: GuidGenerator.Create(),
            productId: productId,
            deviceName: deviceName,
            deviceSecret: DeviceSecretGenerator.Create(),
            remarkName: remarkName,
            isActive: false,
            isEnable: true,
            status: DeviceStatus.Offline,
            description: description
         );

        return device;
    }

    public virtual async Task<Device> UpdateDeviceAsync(
        Device device,
        string deviceName,
        string? remarkName,
        string? description)
    {
        await CheckDeviceNameDuplication(device.ProductId, deviceName, device.Id);

        device.SetDeviceName(deviceName);
        device.SetRemarkName(remarkName);
        device.SetDescription(description);

        return device;
    }

    public async Task CheckDeviceNameDuplication(Guid productId, string deviceName, Guid? ignoreDeviceId = null)
    {
        // 同一产品下设备名称不能重复
        var existingDevice = await DeviceRepository.FindByDeviceNameAsync(productId, deviceName);

        if (existingDevice != null && existingDevice.Id != ignoreDeviceId)
        {
            throw new DuplicateDeviceNameException(deviceName);
        }
    }
}
