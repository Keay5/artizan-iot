using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Artizan.IoT.ThingModels.Tsls.Validators.Caches;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT.ThingModels.Tsls.Validators;

/// <summary>
/// TLS 校验器
/// </summary>
public class DefaultTslValidator : ITslValidator, ISingletonDependency
{
    //TSL Josn schema 在 ABP 虚拟文件系统（ Virtual File System ）的存储路径 
    protected const string TslSchemaFilePath = "Artizan/IoT/Resources/ThingModels/iot-tsl-schema.json";
    protected IVirtualFileProvider VirtualFileProvider;
    protected IDistributedCache<TslJsonSchemaCacheItem> TslJsonSchemaCache;

    public DefaultTslValidator(
        IVirtualFileProvider virtualFileProvider,
        IDistributedCache<TslJsonSchemaCacheItem> tslJsonSchemaCache)
    {
        VirtualFileProvider = virtualFileProvider;
        TslJsonSchemaCache = tslJsonSchemaCache;
    }

    public virtual async Task<(bool IsValid, List<string> Errors)> ValidateAsync(string tslJsonString, bool validateJsonSchema = false)
    {
        if (tslJsonString.IsNullOrWhiteSpace())
        {
            return (false, new List<string>() { $"TSL Json 字符串为空。" });
        }

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(tslJsonString);
        if (tsl == null)
        {
            return (false, new List<string>() { $"无法反序列化 TSL Json 字符串。" });
        }

        return await ValidateAsync(tsl, validateJsonSchema);
    }

    /// <summary>
    /// 验证物模型合法性（包含JSON Schema验证）
    /// </summary>
    /// <param name="tsl">物模型TSL实例</param>
    public virtual async Task<(bool IsValid, List<string> Errors)> ValidateAsync(Tsl tsl, bool validateJsonSchema = false)
    {
        var errors = new List<string>();

        // 1. 基础数据注解验证
        var validationContext = new ValidationContext(tsl);
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(tsl, validationContext, validationResults, true);
        errors.AddRange(validationResults.Select(r => r.ErrorMessage));

        // 2. 标识唯一性验证
        ValidateUniqueIdentifiers(tsl, errors);

        // 3. 数值范围验证
        ValidateNumericRanges(tsl, errors);

        if (validateJsonSchema)
        {
            // 4. JSON Schema 合规性验证
            await ValidateJsonSchemaComplianceAsync(tsl, errors);
        }

        return (errors.Count == 0, errors);
    }

    private void ValidateUniqueIdentifiers(Tsl tsl, List<string> errors)
    {
        // 验证属性标识唯一性
        var propGroups = tsl.Properties?.GroupBy(p => p.Identifier) ?? Enumerable.Empty<IGrouping<string, Property>>();
        foreach (var group in propGroups.Where(g => g.Count() > 1))
        {
            errors.Add($"属性标识重复：{group.Key}（出现{group.Count()}次）");
        }

        // 验证事件标识唯一性
        var eventGroups = tsl.Events?.GroupBy(e => e.Identifier) ?? Enumerable.Empty<IGrouping<string, Event>>();
        foreach (var group in eventGroups.Where(g => g.Count() > 1))
        {
            errors.Add($"事件标识重复：{group.Key}（出现{group.Count()}次）");
        }

        // 验证服务标识唯一性
        var serviceGroups = tsl.Services?.GroupBy(s => s.Identifier) ?? Enumerable.Empty<IGrouping<string, Service>>();
        foreach (var group in serviceGroups.Where(g => g.Count() > 1))
        {
            errors.Add($"服务标识重复：{group.Key}（出现{group.Count()}次）");
        }
    }

    private void ValidateNumericRanges(Tsl tsl, List<string> errors)
    {
        foreach (var prop in tsl.Properties ?? Enumerable.Empty<Property>())
        {
            // 仅验证数值类型（int/float/double）
            if (prop.DataType?.Type is DataTypes.Int32 or DataTypes.Float or DataTypes.Double)
            {
                if (prop.DataType.Specs is NumericSpecs numericSpecs)
                {
                    // 验证min和max是否为有效数值
                    if (double.TryParse(numericSpecs.Min, out double min) &&
                        double.TryParse(numericSpecs.Max, out double max))
                    {
                        if (min > max)
                        {
                            errors.Add($"属性{prop.Identifier}：最小值（{min}）大于最大值（{max}）");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(numericSpecs.Min) && !double.TryParse(numericSpecs.Min, out _))
                        {
                            errors.Add($"属性{prop.Identifier}：最小值（{numericSpecs.Min}）不是有效的数值");
                        }
                        if (!string.IsNullOrEmpty(numericSpecs.Max) && !double.TryParse(numericSpecs.Max, out _))
                        {
                            errors.Add($"属性{prop.Identifier}：最大值（{numericSpecs.Max}）不是有效的数值");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 验证物模型是否符合本地JSON Schema（结合ABP分布式缓存）
    /// </summary>
    protected virtual async Task ValidateJsonSchemaComplianceAsync(
        Tsl tls,
        List<string> errors)
    {
        try
        {
            // 步骤1：序列化 TSL JSON String 为JSON对象
            string tslJsonString = TslSerializer.SerializeObject(tls);
            JObject tslJObject = JObject.Parse(tslJsonString);

            // 步骤2：获取并缓存标准的 JSON Schema（优先从缓存读取）
            JSchema standardJsonSchema;
            var tslJsonSchemaCacheKey = "Iot:ThingModel:Tsl:JsonSchema:Draft07"; // 缓存键

            // 尝试从分布式缓存获取
            var cachedJsonSchema = await TslJsonSchemaCache.GetAsync(tslJsonSchemaCacheKey);
            if (cachedJsonSchema != null)
            {
                // 缓存命中：反序列化缓存的schema
                var jsonSchema = cachedJsonSchema.TslJsonSchema;
                standardJsonSchema = ParseJsonSchema(jsonSchema, errors);
                if (standardJsonSchema == null)
                {
                    return;
                }
            }
            else
            {
                // 缓存未命中：从本地文件加载并缓存
                var standardJsonSchemaString = await ReadLocaTslJsonlSchemaFileAsync(TslSchemaFilePath, errors);
                if (string.IsNullOrEmpty(standardJsonSchemaString))
                {
                    return;
                }

                standardJsonSchema = ParseJsonSchema(standardJsonSchemaString, errors);
                if (standardJsonSchema == null)
                {
                    return;
                }

                // 存入分布式缓存（设置24小时过期）
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(24)
                };

                await TslJsonSchemaCache.SetAsync(
                    tslJsonSchemaCacheKey,
                    new TslJsonSchemaCacheItem { TslJsonSchema = standardJsonSchemaString },
                    cacheOptions);
            }

            // 步骤3：执行schema验证
            if (!tslJObject.IsValid(standardJsonSchema, out IList<string> schemaErrors))
            {
                errors.AddRange(schemaErrors.Select(err => $"JSON Schema 验证失败：{err}"));
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"物模型序列化失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"JSON Schema 验证异常：{ex.Message}");
        }

        return;
    }

    /// <summary>
    /// 从 ABP 虚拟文件系统（ Virtual File System ）中读取 TSL Json Schema
    ///  ABP 虚拟文件系统（ Virtual File System ）：参见：https://abp.io/docs/latest/framework/infrastructure/virtual-file-system
    /// </summary>
    private async Task<string?> ReadLocaTslJsonlSchemaFileAsync(
        string jsonSchemaFilePath,
        List<string> errors)
    {
        try
        {
            byte[] buffer;
            using (var stream = VirtualFileProvider.GetFileInfo(jsonSchemaFilePath).CreateReadStream())
            {
                buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer);
            }
            return Encoding.Default.GetString(buffer);
        }
        catch (IOException ex)
        {
            errors.Add($"读取 {jsonSchemaFilePath} 文件失败：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析schema字符串为JSchema对象
    /// </summary>
    private JSchema ParseJsonSchema(string schemaJson, List<string> errors)
    {
        try
        {
            using var stringReader = new StringReader(schemaJson);
            using var jsonReader = new JsonTextReader(stringReader);

            return JSchema.Load(jsonReader);
        }
        catch (JSchemaException ex)
        {
            errors.Add($"schema解析失败（不符合JSON Schema规范）：{ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            errors.Add($"schema格式错误：{ex.Message}");
            return null;
        }
    }
}