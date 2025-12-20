using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Devices.Dtos;
using Artizan.IoTHub.Permissions;
using Artizan.IoTHub.Products;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Data;

namespace Artizan.IoTHub.Devices
{
    [Authorize(IoTHubPermissions.Devices.Default)]
    public class DeviceAppService :
        IoTHubAppServiceBase,
        IDeviceAppService
    {
        protected IDeviceRepository DeviceRepository { get; }
        protected IProductRepository ProductRepository { get; }
        protected DeviceManager DeviceManager { get; }
        public DeviceCache DeviceCache { get; }


        public DeviceAppService(
            IDeviceRepository deviceRepository,
            IProductRepository productRepository,
            DeviceManager deviceManager,
            DeviceCache deviceCache)
        {
            DeviceRepository = deviceRepository;
            ProductRepository = productRepository;
            DeviceManager = deviceManager;
            DeviceCache = deviceCache;
        }

        public virtual async Task<DeviceDto> GetAsync(Guid id)
        {
            var device = await DeviceRepository.GetAsync(id);
            return ObjectMapper.Map<Device, DeviceDto>(device);
        }

        [Authorize(IoTHubPermissions.Devices.Create)]
        public virtual async Task<DeviceDto> CreateAsync(CreateDeviceInput input)
        {
            var productKey = await ProductRepository.GetProductKeyAsync(input.ProductId);
            var device = await DeviceManager.CreateDeviceAsync(
                productId: input.ProductId,
                deviceName: input.DeviceName,
                remarkName: input.RemarkName
            );
            await DeviceRepository.InsertAsync(device);
            // 预先缓信息，避免缓存穿透。
            await DeviceCache.SetAsync(productKey, device.DeviceName, cacheItem: DeviceCache.Map(device, productKey), considerUow: true);

            return ObjectMapper.Map<Device, DeviceDto>(device);
        }

        [Authorize(IoTHubPermissions.Devices.Update)]
        public virtual async Task<DeviceDto> UpdateAsync(Guid id, UpdateDeviceInput input)
        {
            var device = await DeviceRepository.GetAsync(id);
            var productKey = await ProductRepository.GetProductKeyAsync(device.ProductId);

            device.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

            await DeviceManager.UpdateDeviceAsync(device, input.DeviceName, input.RemarkName, input.Description);
            await DeviceRepository.UpdateAsync(device);

            // 预先缓信息，避免缓存穿透。
            await DeviceCache.ResetAsync(productKey, device.DeviceName, cacheItem: DeviceCache.Map(device, productKey), considerUow: true);

            return ObjectMapper.Map<Device, DeviceDto>(device);
        }

        [Authorize(IoTHubPermissions.Devices.Delete)]
        public virtual async Task DeleteAsync(Guid id)
        {
            var device = await DeviceRepository.GetAsync(id);
            var productKey = await ProductRepository.GetProductKeyAsync(device.ProductId);

            await DeviceRepository.DeleteAsync(id);
            await DeviceCache.ClearAsync(productKey, device.DeviceName, considerUow: true);
        }
    }
}
