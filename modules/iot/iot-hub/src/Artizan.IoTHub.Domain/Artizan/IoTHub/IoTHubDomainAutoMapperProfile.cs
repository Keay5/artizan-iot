using Artizan.IoT.Products.Caches;
using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Products;
using AutoMapper;

namespace Artizan.IoTHub;

public class IoTHubDomainAutoMapperProfile : Profile
{
    public IoTHubDomainAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */


        // 基础字段自动映射（忽略EncryptedDeviceSecret和ProductKey，后续手动赋值）
        CreateMap<Device, DeviceCacheItem>()
            .ForMember(dest => dest.EncryptedDeviceSecret, opt => opt.Ignore())
            .ForMember(dest => dest.ProductKey, opt => opt.Ignore());

        CreateMap<Product, ProductCacheItem>()
            .ForMember(dest => dest.EncryptedProductSecret, opt => opt.Ignore());
    }
}
