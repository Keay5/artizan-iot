using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Devices.Exceptions;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Products.Properties;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Artizan.IoTHub.Products;

public class ProductManager : DomainService
{
    protected IStringLocalizer<IoTHubResource> Localizer { get; }
    protected IProductKeyGenerator ProductKeyGenerator { get; }
    protected IProductSecretGenerator ProductSecretGenerator { get; }
    protected IProductRepository ProductRepository { get; }

    public ProductManager(
    IStringLocalizer<IoTHubResource> localizer,
    IProductKeyGenerator productKeyGenerator,
    IProductSecretGenerator productSecretGenerator,
    IProductRepository productRepository)
    {
        Localizer = localizer;
        ProductKeyGenerator = productKeyGenerator;
        ProductSecretGenerator = productSecretGenerator;
        ProductRepository = productRepository;
    }

    /// <summary>
    /// 创建产品
    /// </summary>
    public virtual async Task<Product> CreateProductAsync(
      string productName,
      ProductCategory category,
      string categoryName,
      ProductNodeTypes nodeType,
      ProductNetworkingModes? networkingMode,
      ProductAccessGatewayProtocol? accessGatewayProtocol,
      ProductDataFormat dataFormat,
      ProductAuthenticationMode authenticationMode,
      bool isEnableDynamicRegistration,
      bool isUsingPrivateCACertificate,
      string? description = null)
    {
        var productKey = ProductKeyGenerator.Create();
        await CheckProductKeyDuplication(productKey);
        await CheckProductNameDuplication(productName);
 
        var product = new Product(
            id: GuidGenerator.Create(),
            productKey: productKey,
            productSecret: ProductSecretGenerator.Create(),
            productName: productName);

        product.SetCategoryName(categoryName)
            .SetDescription(description);

        product.Category = category;
        product.NodeType = nodeType;
        product.AccessGatewayProtocol = accessGatewayProtocol;
        product.DataFormat = dataFormat;
        product.AuthenticationMode = authenticationMode;
        product.IsUsingPrivateCACertificate = isUsingPrivateCACertificate;
        product.IsEnableDynamicRegistration = isEnableDynamicRegistration;
        product.NetworkingMode = networkingMode;
        product.ProductStatus = ProductStatus.Developing;

        return product;
    }

    /// <summary>
    /// 更新产品
    /// </summary>
    public virtual async Task<Product> UpdateProductAsync(
        Product product,
        string productName,
        ProductCategory category,
        string categoryName,
        ProductNodeTypes nodeType,
        ProductNetworkingModes? networkingMode,
        ProductAccessGatewayProtocol? accessGatewayProtocol,
        ProductDataFormat dataFormat,
        ProductAuthenticationMode authenticationMode,
        bool isEnableDynamicRegistration,
        bool isUsingPrivateCACertificate,
        ProductStatus productStatus,
        string? description = null)
    {
        
        await CheckProductNameDuplication(productName, product.Id);

        if (product.ProductStatus == ProductStatus.Published)
        {
            throw new BusinessException(IoTHubErrorCodes.CannotUpdatePulishedProduct)
                .WithData("ProductName", productName);
        }

        product.SetCategoryName(categoryName)
            .SetDescription(description);

        product.Category = category;
        product.NodeType = nodeType;
        product.AccessGatewayProtocol = accessGatewayProtocol;
        product.DataFormat = dataFormat;
        product.AuthenticationMode = authenticationMode;
        product.IsUsingPrivateCACertificate = isUsingPrivateCACertificate;
        product.IsEnableDynamicRegistration = isEnableDynamicRegistration;
        product.NetworkingMode = networkingMode;
        product.ProductStatus = ProductStatus.Developing;

        /*------------------------------------------------------------------------------------------------------------------------------------------
         领域服务的方法应是细粒度的：它需对聚合进行小（但有意义且一致）的改动。随后，应用层将这些小改动组合起来，以执行不同的用例
         领域服务方法不应更新实体——这是一项通用原则。
         如果直接更新实体，最终会进行两次数据库更新操作，效率低下。
         作为另一项最佳实践，请将实体对象作为参数接收（由应用层处理聚合内的变更）。

         var placedProduct = await ProductRepository.UpdateAsync(product);
        ------------------------------------------------------------------------------------------------------------------------------------------*/

        return product;
    }

    public async Task CheckProductKeyDuplication(string productKey, Guid? ignoreProductId = null)
    {
        var existingProduct = await ProductRepository.FindByProductKeyAsync(productKey);

        if (existingProduct != null && existingProduct.Id != ignoreProductId)
        {
            throw new BusinessException(IoTHubErrorCodes.DuplicateProductKey)
                .WithData("ProductKey", productKey);
        }
    }

    public async Task CheckProductNameDuplication(string productName, Guid? ignoreProductId = null)
    {
        var existingProduct = await ProductRepository.FindByNameAsync(productName);

        if (existingProduct != null && existingProduct.Id != ignoreProductId)
        {
            throw new BusinessException(IoTHubErrorCodes.DuplicateProductName)
                .WithData("ProductName", productName);

            // User Friendly Exception:
            // https://abp.io/docs/latest/framework/fundamentals/exception-handling#User-Friendly-Exception
            //throw new UserFriendlyException(Localizer["DuplicateProductNameMessage", productName]);
        }
    }
}
