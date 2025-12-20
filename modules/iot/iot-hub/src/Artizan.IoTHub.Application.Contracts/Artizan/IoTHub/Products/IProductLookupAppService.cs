using Artizan.IoTHub.Products.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Artizan.IoTHub.Products;

public interface IProductLookupAppService : IApplicationService
{
    Task<ListResultDto<ProductLookupDto>> GetListAsync();
}
