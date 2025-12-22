using Artizan.IoTHub.Products.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Artizan.IoTHub.Products;

[RemoteService(Name = IoTHubRemoteServiceConsts.RemoteServiceName)]
[Area(IoTHubRemoteServiceConsts.ModuleName)]
[ControllerName("ProductLookup")]
[Route("api/iot-hub/product-lookup")]
public class ProductLookupController : IoTHubControllerBase, IProductLookupAppService
{
    protected IProductLookupAppService ProductLookupAppService { get; }
    public ProductLookupController(IProductLookupAppService productLookupAppService)
    {
        ProductLookupAppService = productLookupAppService;
    }

    [HttpGet]
    [Route("list/")]
    public virtual async Task<ListResultDto<ProductLookupDto>> GetListAsync()
    {
        return await ProductLookupAppService.GetListAsync();
    }
}
