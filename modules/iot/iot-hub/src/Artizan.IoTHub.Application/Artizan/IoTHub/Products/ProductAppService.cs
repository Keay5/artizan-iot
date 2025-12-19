using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Devices.Caches;
using Artizan.IoTHub.Permissions;
using Artizan.IoTHub.Products.Caches;
using Artizan.IoTHub.Products.Dtos;
using Artizan.IoTHub.Products.Modules;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products;

[Authorize(IoTHubPermissions.Products.Default)]
public class ProductAppService : IoTHubAppServiceBase, IProductAppService
{
    protected IProductRepository ProductRepository;
    protected ProductManager ProductManager;
    protected IProductModuleRepository ProductModuleRepository;
    protected ProductModuleManager ProductModuleManager;
    protected ProductCache ProductCache { get; }
    protected ProductTslCache ProductTslCache { get; }

    public ProductAppService(
        IProductRepository productRepository,
        ProductManager productManager,
        IProductModuleRepository productModuleRepository,
        ProductModuleManager productModuleManager,
        ProductCache productCache,
        ProductTslCache productTslCache)
    {
        ProductRepository = productRepository;
        ProductManager = productManager;
        ProductModuleRepository = productModuleRepository;
        ProductModuleManager = productModuleManager;
        ProductCache = productCache;
        ProductTslCache = productTslCache;
    }

    public virtual async Task<ListResultDto<ProductDto>> GetListAsync()
    {
        var products = await ProductRepository.GetListAsync();

        return new ListResultDto<ProductDto>(
            ObjectMapper.Map<List<Product>, List<ProductDto>>(products)
        );
    }

    public async Task<PagedResultDto<ProductDto>> GetListPagedAsync(PagedAndSortedResultRequestDto input)
    {
        var products = await ProductRepository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? $"{nameof(Product.CreationTime)} desc", includeDetails:false);

        var totalCount = await ProductRepository.GetCountAsync();
        return new PagedResultDto<ProductDto>(
            totalCount,
            ObjectMapper.Map<List<Product>, List<ProductDto>>(products)
        );
    }

    public virtual async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await ProductRepository.GetAsync(id);

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public virtual async Task<ProductDto> GetByProductKeyAsync(string productkey)
    {
        Check.NotNullOrWhiteSpace(productkey, nameof(productkey));
        var product = await ProductRepository.GetByProductKeyAsync(productkey);

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    [Authorize(IoTHubPermissions.Products.Create)]
    public virtual async Task<ProductDto> CreateAsync(CreateProductInput input)
    {
        var newProduct = await ProductManager.CreateProductAsync(
            productName: input.ProductName,
            category: input.Category,
            categoryName: input.CategoryName,
            nodeType: input.NodeType,
            networkingMode: input.NetworkingMode,
            accessGatewayProtocol: input.AccessGatewayProtocol,
            dataFormat: input.DataFormat,
            authenticationMode: input.AuthenticationMode,
            isEnableDynamicRegistration: input.IsEnableDynamicRegistration,
            isUsingPrivateCACertificate: input.IsUsingPrivateCACertificate,
            description: input.Description);
         await ProductRepository.InsertAsync(newProduct);
        // 预先缓存，避免缓存穿透。
        await ProductCache.ResetAsync(newProduct.ProductKey, ProductCache.Map(newProduct), considerUow: true);

        // 创建产品后，得创建一个默认的产品模块
        var newProductModule = await ProductModuleManager.CreateDefaultProductModuleAsync(newProduct.Id, newProduct.ProductKey);
        await ProductModuleRepository.InsertAsync(newProductModule);
        // 预先缓存，避免缓存穿透。
        await ProductTslCache.SetAsync(newProduct.ProductKey, [newProductModule], considerUow: true);

        // TODO: 添加一系列默认主题?

        return ObjectMapper.Map<Product, ProductDto>(newProduct);
    }

    [Authorize(IoTHubPermissions.Products.Update)]
    public virtual async Task<ProductDto> UpdateAsync(Guid id, UpdateProductInput input)
    {
        var product = await ProductRepository.GetAsync(id);

        product.SetProductName(input.ProductName);
        product.SetDescription(input.Description);
        product.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductRepository.UpdateAsync(product);
        // 预先缓存，避免缓存穿透。
        await ProductCache.ResetAsync(product.ProductKey, ProductCache.Map(product), considerUow: true );

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    [Authorize(IoTHubPermissions.Products.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var product = await ProductRepository.GetAsync(id);
        await ProductRepository.DeleteAsync(id);

        await ProductCache.ClearAsync(product.ProductKey, considerUow: true);
        await ProductTslCache.ClearAsync(product.ProductKey, considerUow: true);
    }
}
