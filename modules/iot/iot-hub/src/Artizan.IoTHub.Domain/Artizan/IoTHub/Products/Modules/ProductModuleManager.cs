using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.Exceptions;
using Artizan.IoT.ThingModels.Tsls.MetaDatas;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Artizan.IoT.ThingModels.Tsls.Validators;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Products.ProductMoudles;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Validation;
using static Artizan.IoT.IoTAbstractionsErrorCodes;

namespace Artizan.IoTHub.Products.Modules;

/// <summary>
/// 产品模块：
/// 参考：
/// UI:  https://help.aliyun.com/zh/iot/user-guide/add-a-tsl-feature?spm=a2c4g.11186623.0.i6#undefined
/// API：https://help.aliyun.com/zh/iot/developer-reference/api-a99t11?spm=a2c4g.11186623.help-menu-30520.d_4_0_6_9_0.27cd61daU05P3d
/// </summary>
public class ProductModuleManager : DomainService
{
    protected IStringLocalizer<IoTHubResource> Localizer { get; }
    protected ITslValidator TslValidator { get; }
    protected IProductKeyGenerator ProductKeyGenerator { get; }
    protected IProductMoudelVesionGenerator ProductMoudelVesionGenerator { get; }
    protected IProductModuleRepository ProductModuleRepository { get; }

    public ProductModuleManager(
        IStringLocalizer<IoTHubResource> localizer,
        ITslValidator tslValidator,
        IProductKeyGenerator productKeyGenerator,
        IProductMoudelVesionGenerator productMoudelVesionGenerator,
        IProductModuleRepository productModuleRepository)
    {
        Localizer = localizer;
        TslValidator = tslValidator;
        ProductKeyGenerator = productKeyGenerator;
        ProductMoudelVesionGenerator = productMoudelVesionGenerator;
        ProductModuleRepository = productModuleRepository;
    }

    #region Moudle

    /// <summary>
    /// 创建默认产品模块
    /// </summary>
    public virtual async Task<ProductModule> CreateDefaultProductModuleAsync(Guid productId, string productKey)
    {
        var productModule = await CreateProductModuleAsync(
            productId: productId,
            identifier: TslConsts.Modules.DefaultModuleIdentifier,
            name: Localizer["DefaultModule"],
            isDefault: true,
            description: null);

        return productModule;
    }

    /// <summary>
    /// 创建产品模块
    /// </summary>
    public virtual async Task<ProductModule> CreateProductModuleAsync(
        Guid productId,
        string identifier,
        string name,
        bool isDefault,
        string? description = null)
    {
        await CheckProductModuleIdentifierDuplication(productId, identifier);
        await CheckProductModuleNameDuplication(productId, name);

        var tsl = new Tsl(
            productKey: ProductKeyGenerator.Create(),
            productModuleIdentifier: identifier,
            productModuleName: name,
            isDefault: isDefault,
            version: ProductMoudelVesionGenerator.Create(),
            description: description);

        var tslJsonString = TslSerializer.SerializeObject(tsl);

        var productModule = new ProductModule(
            id: GuidGenerator.Create(),
            productId: productId,
            name: name,
            identifier: identifier,
            isDefault: isDefault,
            status: ProductModuleStatus.Draft,
            version: null,
            isCurrentVersion: false,
            tsl: tslJsonString,
            description: description);

        return productModule;
    }

    /// <summary>
    /// 更新产品模块
    /// </summary>
    public virtual async Task<ProductModule> UpdateProductModuleAsync(
        ProductModule productModule,
        string identifier,
        string name,
        string? description = null)
    {
        var productId = productModule.ProductId;
        var productModuleId = productModule.Id;

        await CheckProductModuleIdentifierDuplication(productId, identifier, productModuleId);
        await CheckProductModuleNameDuplication(productId, name, productModuleId);

        productModule.SetIdentifier(identifier);
        productModule.SetName(name);
        productModule.SetDescription(description);

        // 更新 TSL
        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);
        tsl.SetFunctionBlockId(identifier);
        tsl.SetFunctionBlockName(name);
        var tslJsonString = TslSerializer.SerializeObject(tsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    #endregion

    #region Module Property

    /// <summary>
    /// 产品模块：添加属性
    /// </summary>
    public virtual async Task<ProductModule> AddProductModulePropertyAsync(
        ProductModule productModule,
        string identifier,
        string name,
        AccessModes accessMode,
        bool required,
        DataTypes dataType,
        ISpecsDo specsDo,
        string? description = null)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }

        var property = TslFactory.CreateProperty(
            identifier: identifier,
            name: name,
            accessMode: accessMode,
            required: required,
            dataType: dataType,
            specsDo: specsDo,
            description: description);

        productModuleTsl.Properties.Add(property);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }

        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    /// <summary>
    /// 产品模块：编辑属性
    /// </summary>
    public virtual async Task<ProductModule> EditProductModulePropertyAsync(
        ProductModule productModule,
        string identifier,
        string newIdentifier,
        string name,
        AccessModes accessMode,
        bool required,
        DataTypes dataType,
        ISpecsDo specsDo,
        string? description = null)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }

        productModuleTsl!.RemoveProperty(identifier);
        var newProperty = TslFactory.CreateProperty(
            identifier: newIdentifier,
            name: name,
            accessMode: accessMode,
            required: required,
            dataType: dataType,
            specsDo: specsDo,
            description: description);
        productModuleTsl.AddProperty(newProperty);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }

        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    /// <summary>
    /// 产品模块：删除属性
    /// </summary>
    public virtual async Task<ProductModule> DeleteProductModulePropertyAsync(
        ProductModule productModule,
        string identifier)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }
        productModuleTsl!.RemoveProperty(identifier);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }

        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    #endregion

    #region Moudle Service

    /// <summary>
    /// 产品模块：添加服务
    /// </summary>
    public virtual async Task<ProductModule> AddProductModuleServiceAsync(
        ProductModule productModule,
        string identifier,
        string name,
        ServiceCallTypes callType,
        List<CommonInputParam>? inputDatas,
        List<OutputParam>? outputDatas,
        string? description = null)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize? BussinessExpetion?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }

        var service = TslFactory.CreateService(
            identifier: identifier,
            name: name,
            callType: callType,
            inputDatas: inputDatas,
            outputDatas: outputDatas,
            description: description
        );

        productModuleTsl!.Services.Add(service);
        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }

        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    /// <summary>
    /// 产品模块：编辑服务
    /// </summary>
    public virtual async Task<ProductModule> EditProductModuleServiceAsync(
        ProductModule productModule,
        string identifier,
        string newIdentifier,
        string name,
        ServiceCallTypes callType,
        List<CommonInputParam>? inputDatas,
        List<OutputParam>? outputDatas,
        string? description = null)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize? BussinessExpetion?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }
        
        productModuleTsl!.RemoveService(identifier);

        var newService = TslFactory.CreateService(
            identifier: newIdentifier,
            name: name,
            callType: callType,
            inputDatas: inputDatas,
            outputDatas: outputDatas,
            description: description
        );
        productModuleTsl.AddService(newService);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }
        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    /// <summary>
    /// 产品模块：编辑服务
    /// </summary>
    public virtual async Task<ProductModule> DeleteProductModuleServiceAsync(
        ProductModule productModule,
        string identifier)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize? BussinessExpetion?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }

        productModuleTsl!.RemoveService(identifier);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }
        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);

        return productModule;
    }

    #endregion

    #region Moudle Event

    /// <summary>
    /// 产品模块：添加事件
    /// </summary>
    public async Task AddProductModuleEventAsync(
        ProductModule productModule,
        string identifier,
        string name,
        EventTypes eventType,
        List<OutputParam> outputDatas,
        string? description)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }

        if (productModuleTsl!.Events == null)
        {
            productModuleTsl.Events = new List<Event>();
        }

        var @event = TslFactory.CreateEvent(
            identifier: identifier,
            name: name,
            eventType: eventType,
            outputDatas: outputDatas,
            description: description);

        productModuleTsl.Events.Add(@event);
        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }
        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);
    }

    /// <summary>
    /// 产品模块：编辑事件
    /// </summary>
    public async Task EditProductModuleEventAsync(
        ProductModule productModule,
        string identifier,
        string newIdentifier,
        string name,
        EventTypes eventType,
        List<OutputParam> outputDatas,
        string? description)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }
        productModuleTsl!.RemoveEvent(identifier);

        var newEvent = TslFactory.CreateEvent(
            identifier: newIdentifier,
            name: name,
            eventType: eventType,
            outputDatas: outputDatas,
            description: description
        );
        productModuleTsl.Events.Add(newEvent);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }
        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);
    }

    /// <summary>
    /// 产品模块：删除事件
    /// </summary>
    public async Task DeleteProductModuleEventAsync(
        ProductModule productModule,
        string identifier)
    {
        var productModuleTsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl);

        if (productModuleTsl == null)
        {
            // TODO: localize?
            ThrowValidationException("Tsl of productModule can not be null or empty!", nameof(productModuleTsl));
        }
        productModuleTsl!.RemoveEvent(identifier);

        // TSL 校验
        var (isValid, errors) = await TslValidator.ValidateAsync(productModuleTsl, true);
        if (!isValid)
        {
            throw new TslFormatErrorExeption(string.Join("; ", errors ?? []));
        }
        var tslJsonString = TslSerializer.SerializeObject(productModuleTsl);
        productModule.SetProductModuleTsl(tslJsonString);
    }

    #endregion

    #region 辅助方法

    public async Task CheckProductModuleIdentifierDuplication(Guid productId, string Identifier, Guid? ignoreProductModuleId = null)
    {
        var existingProductModule = await ProductModuleRepository.FindAsync(p => p.ProductId == productId && p.Identifier == Identifier);

        // 产品模块标识符，在产品中具有唯一性
        if (existingProductModule != null && existingProductModule.Id != ignoreProductModuleId)
        {
            throw new BusinessException(IoTHubErrorCodes.DuplicateProductModuleIdentifier)
                .WithData("Identifier", Identifier);
        }
    }

    public async Task CheckProductModuleNameDuplication(Guid productId, string name, Guid? ignoreProductModuleId = null)
    {
        var existingProductModule = await ProductModuleRepository.FindAsync(p => p.ProductId == productId && p.Name == name);

        // 产品模块名称，在产品中具有唯一性
        if (existingProductModule != null && existingProductModule.Id != ignoreProductModuleId)
        {
            throw new BusinessException(IoTHubErrorCodes.DuplicateProductModuleName)
                .WithData("Identifier", name);
        }
    }

    private void ThrowValidationException(string message, string memberName)
    {
        throw new AbpValidationException(message,
            new List<ValidationResult>
            {
                new ValidationResult(message, new[] {memberName})
            });
    }

    #endregion

}
