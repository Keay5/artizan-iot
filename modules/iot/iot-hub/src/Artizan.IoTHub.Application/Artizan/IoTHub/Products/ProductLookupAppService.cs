using Artizan.IoTHub.Products.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Artizan.IoTHub.Products;

public class ProductLookupAppService : IoTHubAppServiceBase, IProductLookupAppService
{
    protected IProductRepository ProductRepository { get; }

    public ProductLookupAppService(IProductRepository productRepository)
    {
        ProductRepository = productRepository;
    }

    public virtual async Task<ListResultDto<ProductLookupDto>> GetListAsync()
    {
        var productQuery = await ProductRepository.GetQueryableAsync();

        var query = from product in productQuery
                    orderby product.CreationTime descending
                    select new ProductLookupDto
                    {
                        Id = product.Id,
                        ProductKey = product.ProductKey,
                        ProductName = product.ProductName
                    };

        var productList = await AsyncExecuter.ToListAsync(query);

        return new ListResultDto<ProductLookupDto>(productList);
    }
}
