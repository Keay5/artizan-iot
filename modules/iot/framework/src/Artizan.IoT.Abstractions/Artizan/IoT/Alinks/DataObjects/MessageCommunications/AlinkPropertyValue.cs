using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// Alink属性值
/// 2.适配TSL全类型 + 兼容两种格式(支持带time字段)
/// 【协议规范】：float/double类型需带小数位，time为UTC毫秒级时间戳
/// </summary>
[JsonConverter(typeof(AlinkPropertyValueConverter))]
public class AlinkPropertyValue
{
    /// <summary>
    /// 属性值（适配TSL所有类型：int/float/double/bool/enum/text/date/array/struct）
    /// </summary>
    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// 上报时间戳（UTC毫秒级，为null时序列化输出简单值格式）
    /// </summary>
    [JsonPropertyName("time")]
    public long? Time { get; set; } = null;

    /// <summary>
    /// 校验值合法性（适配TSL全类型）
    /// </summary>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="tslType">TSL数据类型</param>
    /// <param name="extParams">扩展校验参数（如int范围/枚举项/text长度）</param>
    /// <returns>校验结果</returns>
    public ValidateResult Validate(string propertyIdentifier, DataTypes tslType, Dictionary<string, object>? extParams = null)
    {
        extParams ??= new Dictionary<string, object>();

        // 1. 基础校验：值不能为空（TSL所有类型均不允许空值）
        if (Value == null || (Value is string str && string.IsNullOrWhiteSpace(str)))
        {
            return ValidateResult.Failed($"属性{propertyIdentifier}（{tslType}类型）值不能为空");
        }

        // 2. 按TSL类型细分校验
        return tslType switch
        {
            DataTypes.Int32 => ValidateInt32(propertyIdentifier, extParams),
            DataTypes.Float => ValidateFloat(propertyIdentifier),
            DataTypes.Double => ValidateDouble(propertyIdentifier),
            DataTypes.Boolean => ValidateBoolean(propertyIdentifier),
            DataTypes.Enum => ValidateEnum(propertyIdentifier, extParams),
            DataTypes.Text => ValidateText(propertyIdentifier, extParams),
            DataTypes.Date => ValidateDate(propertyIdentifier),
            DataTypes.Array => ValidateArray(propertyIdentifier, extParams),
            DataTypes.Struct => ValidateStruct(propertyIdentifier),
            _ => ValidateResult.Failed($"属性{propertyIdentifier}的TSL类型{tslType}不支持")
        };
    }

    #region 私有校验方法（按TSL类型细分）
    /// <summary>
    /// 校验Int32类型（支持取值范围）
    /// </summary>
    private ValidateResult ValidateInt32(string propertyId, Dictionary<string, object> extParams)
    {
        if (!NumericConverter.TryConvertToInt32(Value, out var intVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（int类型）值{Value}不是合法整数");
        }

        // 可选：校验取值范围（extParams需包含Min/Max）
        if (extParams.ContainsKey("Min") && intVal < Convert.ToInt32(extParams["Min"]))
        {
            return ValidateResult.Failed($"属性{propertyId}（int类型）值{intVal}小于最小值{extParams["Min"]}");
        }
        if (extParams.ContainsKey("Max") && intVal > Convert.ToInt32(extParams["Max"]))
        {
            return ValidateResult.Failed($"属性{propertyId}（int类型）值{intVal}大于最大值{extParams["Max"]}");
        }

        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Float类型（需带小数位）
    /// </summary>
    private ValidateResult ValidateFloat(string propertyId)
    {
        if (!NumericConverter.TryConvertToFloat(Value, out var floatVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（float类型）值{Value}不是合法单精度浮点数");
        }
        if (floatVal == Math.Floor(floatVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（float类型）值{floatVal}需带小数位（如{floatVal}.0）");
        }
        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Double类型（需带小数位）
    /// </summary>
    private ValidateResult ValidateDouble(string propertyId)
    {
        if (!NumericConverter.TryConvertToDouble(Value, out var doubleVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（double类型）值{Value}不是合法双精度浮点数");
        }
        if (doubleVal == Math.Floor(doubleVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（double类型）值{doubleVal}需带小数位（如{doubleVal}.0）");
        }
        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Boolean类型（兼容0/1/true/false）
    /// </summary>
    private ValidateResult ValidateBoolean(string propertyId)
    {
        if (Value is bool) return ValidateResult.Success();

        // TSL定义：0=假/1=真，兼容字符串/数字
        if (Value is string boolStr)
        {
            if (boolStr == "0" || boolStr == "1" || bool.TryParse(boolStr, out _))
            {
                return ValidateResult.Success();
            }
        }
        if (Value is int boolInt && (boolInt == 0 || boolInt == 1))
        {
            return ValidateResult.Success();
        }

        return ValidateResult.Failed($"属性{propertyId}（bool类型）值{Value}需为0/1/true/false");
    }

    /// <summary>
    /// 校验Enum类型（需在枚举项列表中）
    /// </summary>
    private ValidateResult ValidateEnum(string propertyId, Dictionary<string, object> extParams)
    {
        if (!NumericConverter.TryConvertToInt32(Value, out var enumVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（enum类型）值{Value}不是合法枚举值（需为整数）");
        }

        // extParams需包含EnumItems（枚举项列表，如new List<int> {1,2,3}）
        if (!extParams.ContainsKey("EnumItems"))
        {
            return ValidateResult.Failed($"属性{propertyId}（enum类型）未配置枚举项列表");
        }
        var enumItems = extParams["EnumItems"] as List<int>;
        if (enumItems == null || !enumItems.Contains(enumVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（enum类型）值{enumVal}不在枚举项列表中（{string.Join(",", enumItems)}）");
        }

        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Text类型（长度≤10240字节）
    /// </summary>
    private ValidateResult ValidateText(string propertyId, Dictionary<string, object> extParams)
    {
        if (!(Value is string textVal))
        {
            return ValidateResult.Failed($"属性{propertyId}（text类型）值{Value}不是合法字符串");
        }

        // 计算字节长度（UTF-8）
        var byteLen = Encoding.UTF8.GetByteCount(textVal);
        var maxLen = extParams.ContainsKey("MaxLength") ? Convert.ToInt32(extParams["MaxLength"]) : 10240;

        if (byteLen > maxLen)
        {
            return ValidateResult.Failed($"属性{propertyId}（text类型）字节长度{byteLen}超过最大值{maxLen}（最长10240字节）");
        }

        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Date类型（UTC毫秒时间戳字符串）
    /// </summary>
    private ValidateResult ValidateDate(string propertyId)
    {
        if (!(Value is string dateStr))
        {
            return ValidateResult.Failed($"属性{propertyId}（date类型）值{Value}需为字符串类型的UTC毫秒时间戳");
        }

        if (!long.TryParse(dateStr, out var timestamp))
        {
            return ValidateResult.Failed($"属性{propertyId}（date类型）值{dateStr}不是合法的毫秒时间戳");
        }

        // 校验时间戳合法性（可选：是否为合理范围）
        try
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }
        catch
        {
            return ValidateResult.Failed($"属性{propertyId}（date类型）值{dateStr}不是合法的UTC毫秒时间戳");
        }

        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Array类型（元素类型符合TSL定义）
    /// </summary>
    private ValidateResult ValidateArray(string propertyId, Dictionary<string, object> extParams)
    {
        if (!(Value is IEnumerable<object> array))
        {
            return ValidateResult.Failed($"属性{propertyId}（array类型）值{Value}不是合法数组");
        }

        // extParams需包含ItemType（数组元素的TSL类型）
        if (!extParams.ContainsKey("ItemType"))
        {
            return ValidateResult.Failed($"属性{propertyId}（array类型）未配置元素类型");
        }
        var itemType = (DataTypes)extParams["ItemType"];

        // 校验每个元素的类型
        var index = 0;
        foreach (var item in array)
        {
            var itemResult = new AlinkPropertyValue { Value = item }.Validate($"{propertyId}[{index}]", itemType);
            if (!itemResult.IsValid)
            {
                return itemResult;
            }
            index++;
        }

        return ValidateResult.Success();
    }

    /// <summary>
    /// 校验Struct类型（无嵌套JSON对象）
    /// </summary>
    private ValidateResult ValidateStruct(string propertyId)
    {
        if (!(Value is Dictionary<string, object> dict))
        {
            return ValidateResult.Failed($"属性{propertyId}（struct类型）值{Value}不是合法JSON对象");
        }

        // 校验无嵌套（值不能是数组/对象）
        foreach (var (key, val) in dict)
        {
            if (val is IEnumerable<object> || val is Dictionary<string, object>)
            {
                return ValidateResult.Failed($"属性{propertyId}（struct类型）的子项{key}为嵌套结构（TSL不支持嵌套）");
            }
        }

        return ValidateResult.Success();
    }
    #endregion
}

















