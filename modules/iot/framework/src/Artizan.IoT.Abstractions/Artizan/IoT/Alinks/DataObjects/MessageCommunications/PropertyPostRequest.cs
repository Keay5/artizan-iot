using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.Products.ProductMoudles;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备属性上报请求（原有注释：设备向云端上报属性值，支持单/多属性）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services
/// Topic模板：/sys/${productKey}/${deviceName}/thing/event/property/post
/// Method：thing.event.property.post
public class PropertyPostRequest : AlinkRequestBase
{
    /// <summary>
    /// Alink协议方法名（固定）
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => "thing.event.property.post";

    /// <summary>
    /// 上报属性键值对（Key格式：${模块标识}:${属性标识} 或 属性标识）
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, AlinkPropertyValue> Params { get; set; } = new();

    /// <summary>
    /// 生成上报Topic（固定格式）
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }

        return $"/sys/{productKey}/{deviceName}/thing/event/property/post";
    }

    #region 便捷API（添加属性，适配TSL类型）
    /// <summary>
    /// 添加简单值格式的属性（无time）
    /// </summary>
    public void AddSimpleProperty(string propertyKey, object value)
    {
        Params[propertyKey] = new AlinkPropertyValue { Value = value, Time = null };
    }

    /// <summary>
    /// 添加带time的属性（value+time格式）
    /// </summary>
    public void AddPropertyWithTime(string propertyKey, object value, long time)
    {
        Params[propertyKey] = new AlinkPropertyValue { Value = value, Time = time };
    }
    #endregion

    #region 校验逻辑（适配TSL全类型）
    /// <summary>
    /// 校验所有上报属性（按TSL定义）
    /// </summary>
    /// <param name="tslPropertyMap">属性Key与TSL配置的映射（Key：属性标识/模块:属性标识；Value：(TSL类型, 扩展参数)）</param>
    /// <returns>校验结果</returns>
    public ValidateResult Validate(Dictionary<string, (DataTypes TslType, Dictionary<string, object>? ExtParams)> tslPropertyMap)
    {
        if (Params == null || !Params.Any())
            return ValidateResult.Failed("上报属性不能为空");

        foreach (var (propKey, propValue) in Params)
        {
            // 1. 解析模块标识 + 校验模块规则
            var hasModule = ProductModuleHelper.TryParseModuleFromPropertyKey(propKey, out var moduleId, out var purePropKey);
            if (hasModule && ProductModuleHelper.IsDefaultModule(moduleId))
                return ValidateResult.Failed($"属性Key「{propKey}」禁止使用\"Default\"作为模块标识");
            if (propKey.Contains(":") && !hasModule)
                return ValidateResult.Failed($"属性Key「{propKey}」格式非法（需为${{模块标识}}:${{属性标识}}）");

            // 2. 匹配TSL配置（优先用纯属性Key匹配，兼容模块前缀）
            var targetPropKey = hasModule ? purePropKey : propKey;
            if (!tslPropertyMap.TryGetValue(targetPropKey, out var tslConfig))
            {
                return ValidateResult.Failed($"属性{targetPropKey}未配置TSL类型");
            }

            // 3. 按TSL类型校验属性值
            var valResult = propValue.Validate(propKey, tslConfig.TslType, tslConfig.ExtParams);
            if (!valResult.IsValid) return valResult;

            // 4. 校验time范围（仅当Time有值时）
            if (propValue.Time.HasValue && propValue.Time.Value > DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeMilliseconds())
            {
                return ValidateResult.Failed($"属性{propKey}的time超出未来24小时范围");
            }
        }

        return ValidateResult.Success();
    }
    #endregion
}
