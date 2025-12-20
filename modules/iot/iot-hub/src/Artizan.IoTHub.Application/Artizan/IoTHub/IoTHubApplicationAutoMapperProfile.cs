using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Devices.Dtos;
using Artizan.IoTHub.Products;
using Artizan.IoTHub.Products.Dtos;
using Artizan.IoTHub.Products.Modules;
using Artizan.IoTHub.Products.Modules.Dtos;
using AutoMapper;

namespace Artizan.IoTHub;

public class IoTHubApplicationAutoMapperProfile : Profile
{
    public IoTHubApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Product, ProductDto>();
        CreateMap<ProductModule, ProductModuleDto>();
        CreateMap<Device, DeviceDto>();
    }
}
