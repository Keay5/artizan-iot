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
        // 情况1：JSON是对象（{value, time}）→ 手动解析，避免无限递归，造成死循环内存溢出。
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            object? value = null;
            long? time = null;
            bool isCompleted = false;

            // 手动读取对象内的属性
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    isCompleted = true;
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propName = reader.GetString();
                    reader.Read(); // 移动到属性值

                    switch (propName)
                    {
                        case "value":
                            // 解析value字段（复用之前的简单值解析逻辑）
                            value = ParseValue(ref reader, options);
                            break;
                        case "time":
                            // 解析time字段（必须是数字）
                            if (reader.TokenType == JsonTokenType.Number)
                            {
                                time = reader.GetInt64();
                            }
                            else
                            {
                                throw new JsonException("time字段必须是数字类型");
                            }
                            break;
                        default:
                            // 忽略未知字段
                            reader.Skip();
                            break;
                    }
                }
            }

            if (!isCompleted)
            {
                throw new JsonException("未完成的对象结构");
            }

            return new AlinkPropertyValue
            {
                Value = value ?? string.Empty,
                Time = time
            };
        }

        // 情况2：JSON是简单值 → 按原逻辑解析
        object? simpleValue = ParseValue(ref reader, options);
        return new AlinkPropertyValue { Value = simpleValue ?? string.Empty, Time = null };
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

    #region 辅助方法
    // 提取原逻辑中的值解析逻辑为独立方法，避免重复代码
    private object? ParseValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        // 情况2：JSON是简单值 → 按TSL类型适配解析（核心：解决switch类型推断问题）
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => ParseNumberToTslType(ref reader),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.StartArray => JsonSerializer.Deserialize<object[]>(ref reader, options),
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
            _ => null
        };
    }

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