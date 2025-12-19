using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// Alink属性值JSON转换器（兼容简单值/value+time对象两种格式）
/// 【反序列化】：
/// - 简单值（如"on"/23.6）→ 自动封装为AlinkPropertyValue（Value=简单值，Time=null）；
/// - 复杂对象（{value, time}）→ 正常解析为AlinkPropertyValue；
/// 【序列化】：
/// - Time=null → 直接输出Value的值（如"on"）；
/// - Time≠null → 输出{value, time}对象（如{"value":"on","time":1524448722000}）。
/// </summary>
public class AlinkPropertyValueConverter : JsonConverter<AlinkPropertyValue>
{
    public override AlinkPropertyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 情况1：JSON是对象（{value, time}）→ 正常反序列化
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<AlinkPropertyValue>(ref reader, options) ?? new AlinkPropertyValue();
        }

        // 情况2：JSON是简单值 → 按TSL类型适配解析（核心：解决switch类型推断问题）
        object? value = reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => ParseNumberToTslType(ref reader), // 区分int/float/double
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.StartArray => JsonSerializer.Deserialize<object[]>(ref reader, options), // 数组
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options), // Struct
            _ => null
        };

        return new AlinkPropertyValue { Value = value ?? string.Empty, Time = null };
    }

    public override void Write(Utf8JsonWriter writer, AlinkPropertyValue value, JsonSerializerOptions options)
    {
        // 情况1：Time为null → 输出简单值格式（适配TSL类型）
        if (!value.Time.HasValue)
        {
            WriteSimpleValue(writer, value.Value);
            return;
        }

        // 情况2：Time有值 → 输出{value, time}对象格式
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        WriteSimpleValue(writer, value.Value);
        writer.WriteNumber("time", value.Time.Value);
        writer.WriteEndObject();
    }

    #region 私有辅助方法（适配TSL类型）
    /// <summary>
    /// 解析数字为TSL对应的类型（int/float/double）
    /// </summary>
    private object ParseNumberToTslType(ref Utf8JsonReader reader)
    {
        // 优先判断是否为int（无小数位），否则判断float/double
        if (reader.TryGetInt32(out var intVal))
        {
            return intVal;
        }
        else if (reader.TryGetSingle(out var floatVal))
        {
            return floatVal;
        }
        else
        {
            return reader.GetDouble();
        }
    }

    /// <summary>
    /// 按TSL类型<see cref="ThingModels.Tsls.MetaDatas.Enums.DataTypes"/>序列化简单值（保证输出格式符合TSL定义）
    /// </summary>
    private void WriteSimpleValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case string str:
                writer.WriteStringValue(str);
                break;
            case int intVal:
                writer.WriteNumberValue(intVal);
                break;
            case float floatVal:
                writer.WriteNumberValue(floatVal);
                break;
            case double doubleVal:
                writer.WriteNumberValue(doubleVal);
                break;
            case bool boolVal:
                writer.WriteBooleanValue(boolVal);
                break;
            case object[] array:
                JsonSerializer.Serialize(writer, array);
                break;
            case Dictionary<string, object> dict:
                JsonSerializer.Serialize(writer, dict);
                break;
            default:
                JsonSerializer.Serialize(writer, value);
                break;
        }
    }
    #endregion
}