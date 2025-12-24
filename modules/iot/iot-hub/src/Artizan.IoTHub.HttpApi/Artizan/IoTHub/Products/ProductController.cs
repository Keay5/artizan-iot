using Artizan.IoTHub.Products.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Artizan.IoTHub.Products;

[RemoteService(Name = IoTHubRemoteServiceConsts.RemoteServiceName)]
[Area(IoTHubRemoteServiceConsts.ModuleName)]
[ControllerName("Product")]
[Route("api/iot-hub/products")]
public class ProductController : IoTHubControllerBase, IProductAppService
{
    protected IProductAppService ProductAppService { get;  }

    public ProductController(IProductAppService productAppService)
    {
        ProductAppService = productAppService;
    }

    [HttpGet]
    public async Task<ListResultDto<ProductDto>> GetListAsync()
    {
        return await ProductAppService.GetListAsync();
    }

    [HttpGet]
    [Route("listpaged")]
    public async Task<PagedResultDto<ProductDto>> GetListPagedAsync(PagedAndSortedResultRequestDto input)
    {
        return await ProductAppService.GetListPagedAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ProductDto> GetAsync(Guid id)
    {
        return await ProductAppService.GetAsync(id);
    }

    [HttpGet]
    [Route("byKey/{productKey}")]
    public async Task<ProductDto> GetByProductKeyAsync(string productKey)
    {
        return await ProductAppService.GetByProductKeyAsync(productKey);
    }

    [HttpPost]
    public async Task<ProductDto> CreateAsync(CreateProductInput input)
    {
        return await ProductAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductInput input)
    {
        return await ProductAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await ProductAppService.DeleteAsync(id);
    }
}
