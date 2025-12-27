//using Artizan.IoT.Abstractions.Mqtts.Results;
//using Artizan.IoT.Alinks.DataObjects;
//using Artizan.IoT.Alinks.Results;
//using Artizan.IoT.Alinks.Validators;
//using Artizan.IoT.Mqtts.Dtos;
//using Artizan.IoT.Products.Caches;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Volo.Abp.Caching;

//namespace Artizan.IoT.Mqtts.Validators;

///// <summary>
///// TSL物模型参数校验器（ABP社区版适配，复用现有ProductTslCache）
///// 【设计思路】
///// 1. 核心职责：校验标准化DTO的Params是否符合ProductModule的TSL Schema，仅做校验（单一职责）；
///// 2. 设计原则：
/////    - 职责链模式：按物模型类型（属性/事件/服务）分级校验，简化单方法复杂度；
/////    - 缓存复用：直接复用Artizan.IoTHub的ProductTslCache，避免重复查询数据库；
/////    - 异步设计：适配缓存异步查询，提升高并发下的吞吐量；
///// 3. 核心考量：
/////    - 数据合法性：确保入库数据符合TSL定义，避免脏数据；
/////    - 性能：缓存优先，减少数据库访问，适配高并发；
/////    - 扩展性：新增物模型类型时，仅需新增分支（开闭原则）。
///// 【设计模式】：职责链模式 + 依赖注入模式 + 开闭原则
///// - 职责链：按Method类型分发到不同校验方法，每个方法仅处理一类逻辑；
///// - 依赖注入：注入缓存接口，便于替换缓存实现（如Redis/MemoryCache）。
///// </summary>
//public class TslThingModelValidator : ITslThingModelValidator
//{
//    private readonly ILogger<TslThingModelValidator> _logger;
//    private readonly IDistributedCache<ProductTslCacheItem> _productTslCache;

//    public TslThingModelValidator(
//        ILogger<TslThingModelValidator> logger,
//        IDistributedCache<ProductTslCacheItem> productTslCache)
//    {
//        _logger = logger;
//        _productTslCache = productTslCache;
//    }

//    /// <summary>
//    /// 核心校验方法（异步）
//    /// 【核心流程】：前置校验 → 缓存获取TSL → TSL反序列化 → 职责链校验 → 结果封装
//    /// </summary>
//    public async Task<AlinkHandleResult> ValidateAsync(AlinkDataContext dataContext)
//    {
//        var traceId = dataContext.TraceId;
//        try
//        {
//            _logger.LogDebug("[TraceId:{TraceId}] 开始TSL物模型校验 | ProductKey={PK} | ModuleIdentifier={Module} | Method={Method}",
//                traceId, dataContext.ProductKey, dataContext.ModuleIdentifier, dataContext.Method);

//            // 前置空值快速失败（核心字段缺失直接返回）
//            if (string.IsNullOrWhiteSpace(dataContext.ProductKey))
//            {
//                _logger.LogWarning("[TraceId:{TraceId}] TSL校验失败 | 原因=ProductKey为空", traceId);
//                return AlinkHandleResult.TslValidateFailed(traceId, "ProductKey为空", dataContext);
//            }
//            if (string.IsNullOrWhiteSpace(dataContext.Method))
//            {
//                _logger.LogWarning("[TraceId:{TraceId}] TSL校验失败 | 原因=Method为空", traceId);
//                return AlinkHandleResult.TslValidateFailed(traceId, "Method为空", dataContext);
//            }
//            if (string.IsNullOrWhiteSpace(dataContext.Params))
//            {
//                // 部分Method（如设备上线）允许无Params，直接通过
//                _logger.LogDebug("[TraceId:{TraceId}] TSL校验通过 | 原因=Params为空（允许）", traceId);
//                return AlinkHandleResult.Success(dataContext);
//            }

//            // 从缓存获取TSL（复用Artizan.IoTHub的缓存，提升性能）
//            var cacheKey = ProductTslCacheItem.CalculateCacheKey(dataContext.ProductKey, dataContext.ModuleIdentifier);
//            var tslCacheItem = await _productTslCache.GetAsync(cacheKey);
//            if (tslCacheItem == null || string.IsNullOrWhiteSpace(tslCacheItem.TslContent))
//            {
//                _logger.LogWarning("[TraceId:{TraceId}] TSL校验失败 | 原因=未找到TSL缓存 | CacheKey={CacheKey}",
//                    traceId, cacheKey);
//                return AlinkHandleResult.TslValidateFailed(traceId,
//                    $"未找到ProductKey={dataContext.ProductKey} ModuleIdentifier={dataContext.ModuleIdentifier}的TSL配置",
//                    dataContext);
//            }

//            // TSL反序列化（复用现有Tsl，避免重复定义）
//            var Tsl = JsonSerializer.Deserialize<Tsl>(tslCacheItem.TslContent);
//            if (Tsl == null)
//            {
//                _logger.LogWarning("[TraceId:{TraceId}] TSL校验失败 | 原因=TSL反序列化失败 | CacheKey={CacheKey}",
//                    traceId, cacheKey);
//                return AlinkHandleResult.TslValidateFailed(traceId, "TSL配置格式非法（反序列化失败）", dataContext);
//            }

//            // 职责链分发：按Method类型调用不同校验方法
//            var validateResult = ValidateParamsAgainstTsl(dataContext.Method, dataContext.Params, Tsl);
//            if (!validateResult.IsValid)
//            {
//                _logger.LogWarning("[TraceId:{TraceId}] TSL校验失败 | 原因={Reason} | Params={Params}",
//                    traceId, validateResult.ErrorMessage, dataContext.Params);
//                return AlinkHandleResult.TslValidateFailed(traceId, validateResult.ErrorMessage, dataContext);
//            }

//            _logger.LogInformation("[TraceId:{TraceId}] TSL物模型校验通过 | ProductKey={PK} | Method={Method}",
//                traceId, dataContext.ProductKey, dataContext.Method);
//            return AlinkHandleResult.Success(dataContext);
//        }
//        catch (Exception ex)
//        {
//            // 兜底异常捕获，返回结构化失败结果
//            _logger.LogError(ex, "[TraceId:{TraceId}] TSL物模型校验失败 | 原因=未知异常 | Params={Params}",
//                traceId, dataContext.Params);
//            return AlinkHandleResult.TslValidateFailed(traceId, $"未知异常：{ex.Message}", dataContext);
//        }
//    }

//    /// <summary>
//    /// 职责链核心方法：按Method类型分发校验逻辑
//    /// 【设计考量】：新增物模型类型时，仅需新增分支，无需修改现有逻辑（开闭原则）。
//    /// </summary>
//    private ValidateResult ValidateParamsAgainstTsl(string method, string paramsJson, Tsl Tsl)
//    {
//        var methodSegments = method.Split('.', StringSplitOptions.RemoveEmptyEntries);
//        if (methodSegments.Length < 3)
//        {
//            return ValidateResult.Failed("Method格式非法（需符合：thing.{type}.{name}.post）");
//        }

//        var thingType = methodSegments[1]; // property/event/service
//        var thingName = methodSegments[2]; // 属性名/事件名/服务名

//        // 职责链分发
//        return thingType.ToLower() switch
//        {
//            "property" => ValidatePropertyParams(paramsJson, Tsl),
//            "event" => ValidateEventParams(thingName, paramsJson, Tsl),
//            "service" => ValidateServiceParams(thingName, paramsJson, Tsl),
//            _ => ValidateResult.Failed($"不支持的物模型类型：{thingType}")
//        };
//    }

//    #region 职责链分支方法（分级校验）
//    /// <summary>
//    /// 校验属性上报参数（核心：数据类型+取值范围）
//    /// 【设计考量】：属性是最核心的物模型类型，需严格校验，确保入库数据合法。
//    /// </summary>
//    private ValidateResult ValidatePropertyParams(string paramsJson, Tsl Tsl)
//    {
//        try
//        {
//            var paramsObj = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);
//            if (paramsObj == null)
//            {
//                return ValidateResult.Failed("Params不是合法的JSON对象");
//            }

//            foreach (var (propName, propValue) in paramsObj)
//            {
//                var tslProperty = Tsl.Properties?.FirstOrDefault(p => p.Name == propName);
//                if (tslProperty == null)
//                {
//                    return ValidateResult.Failed($"属性{propName}未在TSL中定义");
//                }

//                // 数据类型校验
//                var typeValidateResult = ValidateDataType(propValue, tslProperty.DataType);
//                if (!typeValidateResult.IsValid)
//                {
//                    return ValidateResult.Failed($"属性{propName}类型错误：{typeValidateResult.ErrorMessage}");
//                }

//                // 取值范围校验（如有）
//                if (tslProperty.Min != null && !CheckMinValue(propValue, tslProperty.Min))
//                {
//                    return ValidateResult.Failed($"属性{propName}值小于最小值{tslProperty.Min}");
//                }
//                if (tslProperty.Max != null && !CheckMaxValue(propValue, tslProperty.Max))
//                {
//                    return ValidateResult.Failed($"属性{propName}值大于最大值{tslProperty.Max}");
//                }
//            }

//            return ValidateResult.Success();
//        }
//        catch (JsonException ex)
//        {
//            return ValidateResult.Failed($"Params JSON解析失败：{ex.Message}");
//        }
//    }

//    /// <summary>
//    /// 校验事件上报参数（仅校验事件存在性）
//    /// 【设计考量】：事件参数灵活性高，仅做基础校验，细节由业务层处理。
//    /// </summary>
//    private ValidateResult ValidateEventParams(string eventName, string paramsJson, Tsl Tsl)
//    {
//        var tslEvent = Tsl.Events?.FirstOrDefault(e => e.Name == eventName);
//        if (tslEvent == null)
//        {
//            return ValidateResult.Failed($"事件{eventName}未在TSL中定义");
//        }
//        return ValidateResult.Success();
//    }

//    /// <summary>
//    /// 校验服务调用参数（仅校验服务存在性）
//    /// 【设计考量】：服务是双向交互，参数校验由服务实现层处理，此处仅做基础校验。
//    /// </summary>
//    private ValidateResult ValidateServiceParams(string serviceName, string paramsJson, Tsl Tsl)
//    {
//        var tslService = Tsl.Services?.FirstOrDefault(s => s.Name == serviceName);
//        if (tslService == null)
//        {
//            return ValidateResult.Failed($"服务{serviceName}未在TSL中定义");
//        }
//        return ValidateResult.Success();
//    }
//    #endregion

//    #region 辅助校验方法（原子逻辑）
//    /// <summary>
//    /// 校验数据类型（适配TSL的DataType枚举）
//    /// 【设计考量】：封装原子校验逻辑，避免重复代码，提升可维护性。
//    /// </summary>
//    private ValidateResult ValidateDataType(object value, string dataType)
//    {
//        return dataType.ToLower() switch
//        {
//            "int" => value is int || (long.TryParse(value.ToString(), out _))
//                ? ValidateResult.Success()
//                : ValidateResult.Failed("需为整数类型"),
//            "float" => value is float || value is double || (double.TryParse(value.ToString(), out _))
//                ? ValidateResult.Success()
//                : ValidateResult.Failed("需为浮点类型"),
//            "string" => value is string || (value.ToString() != null)
//                ? ValidateResult.Success()
//                : ValidateResult.Failed("需为字符串类型"),
//            "bool" => value is bool || (bool.TryParse(value.ToString(), out _))
//                ? ValidateResult.Success()
//                : ValidateResult.Failed("需为布尔类型"),
//            "json" => ValidateResult.Success(), // JSON类型不做深层校验
//            _ => ValidateResult.Failed($"不支持的数据类型：{dataType}")
//        };
//    }

//    /// <summary>
//    /// 校验最小值（兼容数字类型）
//    /// </summary>
//    private bool CheckMinValue(object value, object min)
//    {
//        if (double.TryParse(value.ToString(), out var valueNum) && double.TryParse(min.ToString(), out var minNum))
//        {
//            return valueNum >= minNum;
//        }
//        return true;
//    }

//    /// <summary>
//    /// 校验最大值（兼容数字类型）
//    /// </summary>
//    private bool CheckMaxValue(object value, object max)
//    {
//        if (double.TryParse(value.ToString(), out var valueNum) && double.TryParse(max.ToString(), out var maxNum))
//        {
//            return valueNum <= maxNum;
//        }
//        return true;
//    }
//    #endregion
//}

///// <summary>
///// 内部校验结果（封装模式）
///// 【设计考量】：简化校验结果处理，避免直接返回bool+string。
///// </summary>
//internal class ValidateResult
//{
//    public bool IsValid { get; set; }
//    public string ErrorMessage { get; set; } = string.Empty;

//    public static ValidateResult Success() => new() { IsValid = true };
//    public static ValidateResult Failed(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
//}

///// <summary>
///// TSL校验器接口（接口隔离）
///// </summary>
