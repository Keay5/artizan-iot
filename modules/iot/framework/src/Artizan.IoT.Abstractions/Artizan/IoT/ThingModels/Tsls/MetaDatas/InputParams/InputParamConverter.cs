using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

/// <summary>
/// 支持如下类型的序列化和反序列化：
/// 1.propertySet 服务（对象数组）：
/// [
///   {
///     "identifier": "windSpeed",
///     "name": "风速档位",
///     "dataType": {
///       "type": "int",
///       "specs": {
///         "min": "1",
///         "max": "5",
///         "step": "1",
///         "unit": "gear",
///         "unitName": "档"
///       }
///     },
///     "required": false
///   }
/// ]
/// 
/// 2.propertyGet 服务（字符串数组）:
/// "inputData": ["windSpeed"]
/// 
/// 兼容：混合场景与边界值
///     - 空数组："inputData": []
///     - 单元素对象："inputData": {"identifier":"temp","dataType":{"type":"float"}}
///     - null 值："inputData": null
/// </summary>
public class InputParamConverter : JsonConverter<IInputParam>
{
    /// <summary>
    /// 序列化：将 IInputParam 具体实现类转为 JSON（支持对象/字符串）
    /// </summary>
    public override void WriteJson(JsonWriter writer, IInputParam? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // 根据具体类型分支处理
        switch (value)
        {
            case CommonInputParam commonParam:
                serializer.Serialize(writer, commonParam);
                break;
            case PropertyIdentifierParam idParam:
                // 序列化为纯字符串（而非对象）
                writer.WriteValue(idParam.Identifier);
                break;
            default:
                throw new JsonSerializationException($"不支持的IInputParam类型: {value.GetType().FullName}");
        }
    }

    /// <summary>
    /// 反序列化：从 JSON（对象/字符串）还原为 IInputParam 具体实现类
    /// </summary>
    public override IInputParam? ReadJson(JsonReader reader, Type objectType, IInputParam? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        // 新增：支持直接从 JToken 读取（兼容数组转换器嵌套调用）
        if (reader is JTokenReader jTokenReader && jTokenReader.CurrentToken != null)
        {
            return ReadFromJToken(jTokenReader.CurrentToken, serializer);
        }

        // 处理字符串类型（如 "windSpeed"）
        if (reader.TokenType == JsonToken.String)
        {
            var identifier = reader.Value?.ToString() ?? string.Empty;
            return new PropertyIdentifierParam { Identifier = identifier };
        }

        // 处理对象类型（如 { "identifier": "windSpeed", "dataType": {...} }）
        if (reader.TokenType == JsonToken.StartObject)
        {
            var jObject = JObject.Load(reader);

            // 通过字段特征判断具体类型：CommonInputParam 包含 DataType / Name
            if (jObject.ContainsKey("DataType") && jObject.ContainsKey("Name"))
            {
                return jObject.ToObject<CommonInputParam>(serializer);
            }

            // 否则是 PropertyIdentifierParam（兼容旧对象格式）
            return jObject.ToObject<PropertyIdentifierParam>(serializer);
        }
        // 容错：尝试读取为 JToken 后处理
        try
        {
            var jToken = JToken.Load(reader);
            return ReadFromJToken(jToken, serializer);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"不支持的JSON类型: {reader.TokenType}", ex);
        }
    }

    /// <summary>
    /// 从 JToken 反序列化（复用逻辑）
    /// </summary>
    private IInputParam? ReadFromJToken(JToken jToken, JsonSerializer serializer)
    {
        if (jToken.Type == JTokenType.String)
        {
            return new PropertyIdentifierParam
            {
                Identifier = jToken.Value<string>() ?? string.Empty
            };
        }
        else if (jToken.Type == JTokenType.Object)
        {
            var jObject = (JObject)jToken;
            if (jObject.ContainsKey("DataType") && jObject.ContainsKey("Name"))
            {
                return jObject.ToObject<CommonInputParam>(serializer);
            }
            return jObject.ToObject<PropertyIdentifierParam>(serializer);
        }
        return null;
    }
}