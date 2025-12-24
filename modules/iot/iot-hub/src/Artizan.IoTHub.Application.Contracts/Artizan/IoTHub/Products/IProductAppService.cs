using Artizan.IoTHub.Products.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Artizan.IoTHub.Products;

public interface IProductAppService : IApplicationService
{
    Task<ListResultDto<ProductDto>> GetListAsync();
    Task<PagedResultDto<ProductDto>> GetListPagedAsync(PagedAndSortedResultRequestDto input);
    Task<ProductDto> GetAsync(Guid id);
    Task<ProductDto> GetByProductKeyAsync(string productKey);
    Task<ProductDto> CreateAsync(CreateProductInput input);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductInput input);
    Task DeleteAsync(Guid id);
}
