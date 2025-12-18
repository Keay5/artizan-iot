using Artizan.IoTHub.Products.Modules;
using Artizan.IoTHub.Products.Modules.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Artizan.IoTHub.Products;

[RemoteService(Name = IoTHubRemoteServiceConsts.RemoteServiceName)]
[Area(IoTHubRemoteServiceConsts.ModuleName)]
[ControllerName("ProductModule")]
[Route("api/iot-hub/product-modules")]
public class ProductModuleController : IoTHubControllerBase, IProductModuleAppService
{
    protected IProductModuleAppService ProductModuleAppService;

    #region Moudle

    public ProductModuleController(IProductModuleAppService productModuleAppService)
    {
        ProductModuleAppService = productModuleAppService;
    }

    [HttpGet]
    [Route("current-version")]
    public virtual async Task<ListResultDto<ProductModuleDto>> GetCurrentVersionListAsync(Guid productId)
    {
        return await ProductModuleAppService.GetCurrentVersionListAsync(productId);
    }

    [HttpPost]
    public virtual async Task<ProductModuleDto> CreateProductModuleAsync(CreateProductModuleInput input)
    {
        return await ProductModuleAppService.CreateProductModuleAsync(input);
    }

    [HttpPut]
    [Route("{productModuleId}")]
    public virtual async Task<ProductModuleDto> UpdateProductModuleAsync(Guid productModuleId, UpdateProductModuleInput input)
    {
        return await ProductModuleAppService.UpdateProductModuleAsync(productModuleId, input);
    }

    [HttpDelete]
    [Route("{productModuleId}")]
    public virtual async Task DeleteProductModuleAsync(Guid productModuleId)
    {
        await ProductModuleAppService.DeleteProductModuleAsync(productModuleId);
    }

    #endregion

    #region Module Property

    [HttpPost]
    [Route("{productModuleId}/properties")]
    public virtual async Task<ProductModuleDto> CreateProductModulePropertyAsync(Guid productModuleId, CreateProductModulePropertyInput input)
    {
        return await ProductModuleAppService.CreateProductModulePropertyAsync(productModuleId, input);
    }

    [HttpPut]
    [Route("{productModuleId}/properties/{identifier}")]
    public virtual async Task<ProductModuleDto> UpdateProductModulePropertyAsync(Guid productModuleId, string identifier, UpdateProductModulePropertyInput input)
    {
        return await ProductModuleAppService.UpdateProductModulePropertyAsync(productModuleId, identifier, input);
    }

    [HttpDelete]
    [Route("{productModuleId}/properties/{identifier}")]
    public virtual async Task<ProductModuleDto> DeleteProductModulePropertyAsync(Guid productModuleId, string identifier)
    {
        return await ProductModuleAppService.DeleteProductModulePropertyAsync(productModuleId, identifier);
    }

    #endregion

    #region Module Service

    [HttpPost]
    [Route("{productModuleId}/services")]
    public virtual async Task<ProductModuleDto> CreateProductModuleServiceAsync(Guid productModuleId, CreateProductModuleServiceInput input)
    {
        return await ProductModuleAppService.CreateProductModuleServiceAsync(productModuleId, input);
    }

    [HttpPut]
    [Route("{productModuleId}/services/{identifier}")]
    public virtual async Task<ProductModuleDto> UpdateProductModuleServiceAsync(Guid productModuleId, string identifier, UpdateProductModuleServiceInput input)
    {
        return await ProductModuleAppService.UpdateProductModuleServiceAsync(productModuleId, identifier, input);
    }

    [HttpDelete]
    [Route("{productModuleId}/services/{identifier}")]
    public virtual async Task<ProductModuleDto> DeleteProductModuleServiceAsync(Guid productModuleId, string identifier)
    {
        return await ProductModuleAppService.DeleteProductModuleServiceAsync(productModuleId, identifier);
    }

    #endregion

    #region Module Event

    [HttpPost]
    [Route("{productModuleId}/events")]
    public virtual async Task<ProductModuleDto> CreateProductModuleEventAsync(Guid productModuleId, CreateProductModuleEventInput input)
    {
        return await ProductModuleAppService.CreateProductModuleEventAsync(productModuleId, input);
    }

    [HttpPut]
    [Route("{productModuleId}/events/{identifier}")]
    public virtual async Task<ProductModuleDto> UpdateProductModuleEventAsync(Guid productModuleId, string identifier, UpdateProductModuleEventInput input)
    {
        return await ProductModuleAppService.UpdateProductModuleEventAsync(productModuleId, identifier, input);
    }

    [HttpDelete]
    [Route("{productModuleId}/events/{identifier}")]
    public virtual async Task<ProductModuleDto> DeleteProductModuleEventAsync(Guid productModuleId, string identifier)
    {
        return await ProductModuleAppService.DeleteProductModuleEventAsync(productModuleId, identifier);
    }


    #endregion
}
