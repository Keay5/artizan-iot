using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.Products.ProductMoudles;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备属性设置请求（云端→设备）
/// 【协议约束】：
/// 1. Topic模板：/sys/${productKey}/${deviceName}/thing/service/property/set
/// 2. Method固定：thing.service.property.set
/// 3. Params格式：
///    - 默认模块（Default）属性：Key为属性标识（如"temperature"）
///    - 自定义模块属性：Key为${模块标识}:${属性标识}（如"test:temperature"）
/// 4. 数据类型必须严格匹配TSL定义，不允许非法类型/格式
/// 【设计理念】：
/// 1. 继承抽象基类AlinkRequestBase，遵循里氏替换原则，保证Alink协议请求的一致性
/// 2. 复用AlinkPropertyValue的TSL校验能力，避免重复开发（依赖倒置原则）
/// 3. 贴合协议原生格式设计，降低用户理解成本；内置校验逻辑提前拦截非法参数（防御性编程）
/// 4. 单一职责：仅负责“属性设置请求”的构建、校验、Topic生成，不承担其他业务逻辑
/// </summary>
public class PropertySetRequest : AlinkRequestBase
{
    /// <summary>
    /// 协议方法名（固定值，符合阿里云Alink协议规范）
    /// 【设计考量】：重写抽象属性，强制绑定当前场景的固定Method，避免用户手动修改导致错误
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => "thing.service.property.set";

    /// <summary>
    /// 属性设置参数集（完全贴合阿里云协议格式）
    /// Key规则：
    /// - 默认模块（Default）属性：直接使用属性标识（如"temperature"）
    /// - 自定义模块属性：${模块标识}:${属性标识}（如"test:temperature"）
    /// Value类型：AlinkPropertyValue（封装值，支持TSL全类型校验）
    /// 【设计考量】：
    /// 1. 替换原Dictionary<string, object>，通过AlinkPropertyValue强绑定TSL校验规则
    /// 2. 默认初始化空字典，避免空引用异常；序列化格式完全符合Alink协议要求
    /// 3. 不额外封装moduleId参数，直接遵循协议原生Key格式，减少理解偏差
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, AlinkPropertyValue> Params { get; set; } = new();

    /// <summary>
    /// 生成符合协议规范的Topic
    /// 【设计考量】：
    /// 1. 校验ProductKey/DeviceName非空，提前拦截无效参数（防御性编程）
    /// 2. 封装Topic格式，避免用户手动拼接导致的协议错误，降低适配成本
    /// </summary>
    /// <param name="productKey">产品标识（必填，阿里云IoT平台分配）</param>
    /// <param name="deviceName">设备标识（必填，产品下唯一）</param>
    /// <returns>完整的属性设置Topic字符串</returns>
    /// <exception cref="ArgumentNullException">ProductKey/DeviceName为空时抛出</exception>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空（协议约束：Topic必须包含产品标识）");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空（协议约束：Topic必须包含设备标识）");
        }
        return $"/sys/{productKey}/{deviceName}/thing/service/property/set";
    }

    #region 便捷API（贴合协议格式，降低使用成本）
    /// <summary>
    /// 添加属性（支持默认模块和自定义模块，直接遵循协议Key格式）
    /// 【使用说明】：
    /// - 默认模块属性：propKey传入属性标识（如"temperature"）
    /// - 自定义模块属性：propKey传入${模块标识}:${属性标识}（如"test:temperature"）
    /// 【设计考量】：
    /// 1. 封装AlinkPropertyValue的初始化，用户无需关注内部结构
    /// 2. 不额外拆分moduleId参数，完全贴合协议原生格式，减少理解和使用成本
    /// 3. 入参校验提前拦截无效Key，避免后续校验冗余
    /// </summary>
    /// <param name="propKey">属性Key（遵循协议格式：默认模块用属性标识，自定义模块用${模块标识}:${属性标识}）</param>
    /// <param name="value">属性值（需符合对应TSL类型格式）</param>
    public void AddProperty(string propKey, object value)
    {
        if (string.IsNullOrWhiteSpace(propKey))
        {
            throw new ArgumentException("属性Key不能为空", nameof(propKey));
        }
        // 提前拦截非法分隔符（避免用户误用水印冒号等）
        if (propKey.Contains("："))
        {
            throw new ArgumentException($"属性Key「{propKey}」格式非法，模块属性分隔符需使用英文冒号（:），而非中文冒号（：）", nameof(propKey));
        }
        Params[propKey] = new AlinkPropertyValue { Value = value, Time = null };
    }
    #endregion

    #region TSL校验核心逻辑（确保params合法性，贴合协议要求）
    /// <summary>
    /// 基于TSL定义校验所有属性的合法性（覆盖协议全约束）
    /// 【校验维度】：
    /// 1. Params非空校验；
    /// 2. 属性Key格式校验（自定义模块属性需符合${模块标识}:${属性标识}，分隔符为英文冒号）；
    /// 3. 属性Key合法性：禁止显式使用Default作为模块标识（默认模块无需指定）；
    /// 4. 属性存在性校验（校验属性标识是否在TSL配置中定义）；
    /// 5. 数据类型校验（基于TSL的DataTypes校验值格式/范围/枚举项等）；
    /// 【设计考量】：
    /// 1. 依赖TSL配置映射，实现“配置驱动校验”，支持灵活适配不同设备的TSL定义（开闭原则）；
    /// 2. 复用AlinkPropertyValue.Validate方法的TSL全类型校验能力，避免重复编码；
    /// 3. 详细错误信息，便于问题定位（包含属性Key、错误原因）；
    /// 4. 校验失败直接返回结果，不继续执行，提升效率；
    /// </summary>
    /// <param name="tslPropertyMap">
    /// TSL属性配置映射：Key=纯属性标识（无模块）；Value=（TSL数据类型，扩展校验参数）
    /// 扩展参数说明：
    /// - Int32/Float/Double：Min（最小值）、Max（最大值）
    /// - Enum：EnumItems（枚举项列表，List<int>）
    /// - Text：MaxLength（最大字节长度，默认10240）
    /// - Array：ItemType（数组元素TSL类型，DataTypes）
    /// </param>
    /// <returns>校验结果（成功/失败+错误信息）</returns>
    public ValidateResult Validate(Dictionary<string, (DataTypes TslType, Dictionary<string, object>? ExtParams)> tslPropertyMap)
    {
        // 1. 基础校验：Params不能为空（协议约束：属性设置必须携带参数）
        if (Params == null || !Params.Any())
        {
            return ValidateResult.Failed("属性设置参数不能为空（Params字段为必填）");
        }

        // 2. 校验TSL配置映射非空
        if (tslPropertyMap == null || !tslPropertyMap.Any())
        {
            return ValidateResult.Failed("TSL属性配置不能为空，无法完成参数校验");
        }

        // 3. 遍历每个属性进行精细化校验
        foreach (var (propKey, propValue) in Params)
        {
            // 3.1 解析属性Key（区分默认模块/自定义模块，兼容协议两种格式）
            bool hasModule = ProductModuleHelper.TryParseModuleFromPropertyKey(propKey, out string? moduleId, out string purePropKey);

            // 3.2 模块属性格式校验：含冒号但解析失败则格式非法（如多冒号、仅冒号等）
            if (propKey.Contains(":") && !hasModule)
            {
                return ValidateResult.Failed($"属性Key「{propKey}」格式非法，自定义模块属性需符合「${{模块标识}}:${{属性标识}}」格式（例：test:temperature），且仅允许1个英文冒号分隔");
            }

            // 3.3 模块标识合法性校验：禁止显式使用Default作为模块标识（默认模块无需指定，协议约束）
            if (hasModule && string.Equals(moduleId, ProductModuleConsts.DefaultModuleIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return ValidateResult.Failed($"属性Key「{propKey}」禁止显式使用\"Default\"作为模块标识（默认模块属性直接使用属性标识即可，无需添加模块前缀）");
            }

            // 3.4 属性存在性校验：校验纯属性标识是否在TSL配置中定义
            if (!tslPropertyMap.TryGetValue(purePropKey, out var tslConfig))
            {
                return ValidateResult.Failed($"属性「{purePropKey}」未在TSL中定义，无法进行属性设置");
            }

            // 3.5 基于TSL类型校验属性值合法性（复用AlinkPropertyValue的校验能力）
            ValidateResult valueValidateResult = propValue.Validate(propKey, tslConfig.TslType, tslConfig.ExtParams);
            if (!valueValidateResult.IsValid)
            {
                return valueValidateResult;
            }
        }

        // 所有校验通过
        return ValidateResult.Success();
    }
    #endregion
}