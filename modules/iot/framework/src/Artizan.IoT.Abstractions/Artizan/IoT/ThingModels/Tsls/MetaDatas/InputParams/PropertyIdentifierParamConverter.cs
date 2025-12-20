using Newtonsoft.Json;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

// 自定义转换器（保持不变）
public class PropertyIdentifierParamConverter : JsonConverter<PropertyIdentifierParam>
{
    public override void WriteJson(JsonWriter writer, PropertyIdentifierParam value, JsonSerializer serializer)
    {
        // 仅输出 Identifier 字符串
        writer.WriteValue(value?.Identifier);
    }

    public override PropertyIdentifierParam ReadJson(JsonReader reader, Type objectType, PropertyIdentifierParam existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new PropertyIdentifierParam
        {
            Identifier = reader.Value?.ToString()
        };
    }
}
