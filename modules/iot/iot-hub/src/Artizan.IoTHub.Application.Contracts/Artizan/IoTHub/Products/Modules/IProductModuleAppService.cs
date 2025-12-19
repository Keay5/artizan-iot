using Artizan.IoTHub.Products.Modules.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Artizan.IoTHub.Products.Modules;
                   
public interface IProductModuleAppService : IApplicationService
{
    Task<ListResultDto<ProductModuleDto>> GetCurrentVersionListAsync(Guid productId);
    Task<ProductModuleDto> CreateProductModuleAsync(CreateProductModuleInput input);
    Task<ProductModuleDto> UpdateProductModuleAsync(Guid productModuleId, UpdateProductModuleInput input);
    Task DeleteProductModuleAsync(Guid productModuleId);

    Task<ProductModuleDto> CreateProductModulePropertyAsync(Guid productModuleId, CreateProductModulePropertyInput input);
    Task<ProductModuleDto> UpdateProductModulePropertyAsync(Guid productModuleId, string identifier, UpdateProductModulePropertyInput input);
    Task<ProductModuleDto> DeleteProductModulePropertyAsync(Guid productModuleId, string identifier);

    Task<ProductModuleDto> CreateProductModuleServiceAsync(Guid productModuleId, CreateProductModuleServiceInput input);
    Task<ProductModuleDto> UpdateProductModuleServiceAsync(Guid productModuleId, string identifier, UpdateProductModuleServiceInput input);
    Task<ProductModuleDto> DeleteProductModuleServiceAsync(Guid productModuleId, string identifier);

    Task<ProductModuleDto> CreateProductModuleEventAsync(Guid productModuleId, CreateProductModuleEventInput input);
    Task<ProductModuleDto> UpdateProductModuleEventAsync(Guid productModuleId, string identifier, UpdateProductModuleEventInput input);
    Task<ProductModuleDto> DeleteProductModuleEventAsync(Guid productModuleId, string identifier);
}
