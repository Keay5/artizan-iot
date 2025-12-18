using Artizan.IoT.ThingModels.Tsls.Extensions;
using Artizan.IoTHub.Permissions;
using Artizan.IoTHub.Products.Caches;
using Artizan.IoTHub.Products.Modules.Dtos;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;

namespace Artizan.IoTHub.Products.Modules;

[Authorize(IoTHubPermissions.Products.Modules.Default)]
public class ProductModuleAppService : IoTHubAppServiceBase, IProductModuleAppService
{
    protected IProductRepository ProductRepository;
    protected IProductModuleRepository ProductModuleRepository;
    protected ProductModuleManager ProductModuleManager;
    protected ProductTslCache ProductTslCache { get; }    

    public ProductModuleAppService(
        IProductRepository productRepository,
        IProductModuleRepository productModuleRepository,
        ProductModuleManager productModuleManager,
        ProductTslCache productTslCache)
    {
        ProductRepository = productRepository;
        ProductModuleRepository = productModuleRepository;
        ProductModuleManager = productModuleManager;
        ProductTslCache = productTslCache;
    }

    #region Module

    /// <summary>
    /// 添加产品模块。
    /// 用例：https://help.aliyun.com/zh/iot/user-guide/add-a-tsl-feature?scm=20140722.S_help%40%40%E6%96%87%E6%A1%A3%40%4088241._.ID_help%40%40%E6%96%87%E6%A1%A3%40%4088241-RL_%E4%BA%A7%E5%93%81%E6%A8%A1%E5%9D%97%E6%A0%87%E8%AF%86-LOC_doc%7EUND%7Eab-OR_ser-PAR1_212a5d3d17636555027928711ddb6d-V_4-PAR3_o-RE_new5-P0_1-P1_0&spm=a2c4g.11186623.help-search.i19
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>

    [Authorize(IoTHubPermissions.Products.Modules.Management)]
    public virtual async Task<ListResultDto<ProductModuleDto>> GetCurrentVersionListAsync(Guid productId)
    {
        var productModules = await ProductModuleRepository.GetListAsync(
            p => p.ProductId == productId && p.IsCurrentVersion
);
        return new ListResultDto<ProductModuleDto>(
            ObjectMapper.Map<List<ProductModule>, List<ProductModuleDto>>(productModules)
        );
    }

    [Authorize(IoTHubPermissions.Products.Modules.Create)]
    public virtual async Task<ProductModuleDto> CreateProductModuleAsync(CreateProductModuleInput input)
    {
        var product = await ProductRepository.GetAsync(input.ProductId);

        var productModule = await ProductModuleManager.CreateProductModuleAsync(
            productId: product.Id,
            identifier: input.Identifier,
            name: input.Name,
            isDefault: false,
            description: input.Description);

        await ProductModuleRepository.InsertAsync(productModule);
        // 预先缓存，避免缓存穿透。
        await ProductTslCache.SetAsync(product.ProductKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Update)]
    public virtual async Task<ProductModuleDto> UpdateProductModuleAsync(Guid productModuleId, UpdateProductModuleInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        await ProductModuleManager.UpdateProductModuleAsync(
            productModule: productModule,
            identifier: input.Identifier,
            name: input.Name,
            description: input.Description);
        await ProductModuleRepository.UpdateAsync(productModule);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Delete)]
    public virtual async Task DeleteProductModuleAsync(Guid productModuleId)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        await ProductModuleRepository.DeleteAsync(productModuleId, autoSave: true);
        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);
    }

    #endregion

    #region Module Property

    [Authorize(IoTHubPermissions.Products.Modules.Properties.Create)]
    public virtual async Task<ProductModuleDto> CreateProductModulePropertyAsync(Guid productModuleId, CreateProductModulePropertyInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductModuleManager.AddProductModulePropertyAsync(
            productModule: productModule,
            identifier: input.Identifier,
            name: input.Name,
            accessMode: input.AccessMode,
            required: input.Required,
            dataType: input.DataType,
            specsDo: input.SpecsDo,
            description: input.Description
        );

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Properties.Update)]
    public virtual async Task<ProductModuleDto> UpdateProductModulePropertyAsync(Guid productModuleId, string identifier, UpdateProductModulePropertyInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductModuleManager.EditProductModulePropertyAsync(
            productModule: productModule,
            identifier: identifier,
            newIdentifier: input.Identifier,
            name: input.Name,
            accessMode: input.AccessMode,
            required: input.Required,
            dataType: input.DataType,
            specsDo: input.SpecsDo,
            description: input.Description
        );

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Properties.Delete)]
    public virtual async Task<ProductModuleDto> DeleteProductModulePropertyAsync(Guid productModuleId, string identifier)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        await ProductModuleManager.DeleteProductModulePropertyAsync(
            productModule: productModule,
            identifier: identifier
        );

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    #endregion

    #region  Module Service 

    [Authorize(IoTHubPermissions.Products.Modules.Services.Create)]
    public virtual async Task<ProductModuleDto> CreateProductModuleServiceAsync(Guid productModuleId, CreateProductModuleServiceInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

       await ProductModuleManager.AddProductModuleServiceAsync(
            productModule: productModule,
            identifier: input.Identifier,
            name: input.Name,
            callType: input.CallType,
            inputDatas: input.InputDatas?.ConvertToInputParams(),
            outputDatas: input.OutputDatas?.ConvertToOutputParams(),
            description: input.Description
         );

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Services.Update)]
    public virtual async Task<ProductModuleDto> UpdateProductModuleServiceAsync(Guid productModuleId, string identifier, UpdateProductModuleServiceInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductModuleManager.EditProductModuleServiceAsync(
            productModule: productModule,
            identifier: identifier,
            newIdentifier: input.Identifier,
            name: input.Name,
            callType: input.CallType,
            inputDatas: input.InputDatas?.ConvertToInputParams(),
            outputDatas: input.OutputDatas?.ConvertToOutputParams(),
            description: input.Description);

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Services.Delete)]
    public virtual async Task<ProductModuleDto> DeleteProductModuleServiceAsync(Guid productModuleId, string identifier)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        await ProductModuleManager.DeleteProductModuleServiceAsync(
            productModule: productModule,
            identifier: identifier
         );

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    #endregion

    #region Module Event

    [Authorize(IoTHubPermissions.Products.Modules.Events.Create)]
    public virtual async Task<ProductModuleDto> CreateProductModuleEventAsync(Guid productModuleId, CreateProductModuleEventInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductModuleManager.AddProductModuleEventAsync(
            productModule: productModule,
            identifier: input.Identifier,
            name: input.Name,
            eventType: input.EventType,
            outputDatas: input.OutputDatas.ConvertToOutputParams(),
            description: input.Description);

        await ProductModuleRepository.UpdateAsync(productModule);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Events.Update)]
    public virtual async Task<ProductModuleDto> UpdateProductModuleEventAsync(Guid productModuleId, string identifier, UpdateProductModuleEventInput input)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        productModule.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);

        await ProductModuleManager.EditProductModuleEventAsync(
            productModule: productModule,
            identifier: identifier,
            newIdentifier: input.Identifier,
            name: input.Name,
            eventType: input.EventType,
            outputDatas: input.OutputDatas.ConvertToOutputParams(),
            description: input.Description);

        await ProductModuleRepository.UpdateAsync(productModule, autoSave: true);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    [Authorize(IoTHubPermissions.Products.Modules.Events.Delete)]
    public virtual async Task<ProductModuleDto> DeleteProductModuleEventAsync(Guid productModuleId, string identifier)
    {
        var productModule = await ProductModuleRepository.GetAsync(productModuleId);
        var productKey = await ProductRepository.GetProductKeyAsync(productModule.ProductId);

        await ProductModuleManager.DeleteProductModuleEventAsync(
            productModule: productModule,
            identifier: identifier);

        await ProductModuleRepository.UpdateAsync(productModule);

        // 预先缓存，避免缓存穿透。
        await ProductTslCache.ResetAsync(productKey, [productModule], considerUow: true);

        return ObjectMapper.Map<ProductModule, ProductModuleDto>(productModule);
    }

    #endregion

}
