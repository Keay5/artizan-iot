using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;

public class DataTypeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DataType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        // 解析type字段（兼容StringEnumConverter）
        Enums.DataTypes type;
        try
        {
            // 使用Newtonsoft内置的枚举转换逻辑
            type = jObject["type"].ToObject<Enums.DataTypes>(serializer);
        }
        catch (Exception ex)
        {
            var typeStr = jObject["type"]?.ToString() ?? "null";
            throw new JsonSerializationException($"无效的类型: {typeStr}（支持的类型：int/float/bool/enum/text/date/array/struct）", ex);
        }

        var dataType = new DataType { Type = type };

        // 解析specs字段
        var specsToken = jObject["specs"];
        if (specsToken != null && specsToken.Type != JTokenType.Null)
        {
            dataType.Specs = type switch
            {
                Enums.DataTypes.Int32 or Enums.DataTypes.Float or Enums.DataTypes.Double => specsToken.ToObject<NumericSpecs>(serializer),
                Enums.DataTypes.Boolean or Enums.DataTypes.Enum => specsToken.ToObject<KeyValueSpecs>(serializer),
                Enums.DataTypes.Text => specsToken.ToObject<StringSpecs>(serializer),
                Enums.DataTypes.Array => specsToken.ToObject<ArraySpecs>(serializer),
                Enums.DataTypes.Struct => specsToken.ToObject<StructSpecs>(serializer),
                Enums.DataTypes.Date => new EmptySpecs(),
                _ => throw new NotSupportedException($"不支持的类型: {type}")
            };
        }

        return dataType;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var dataType = (DataType)value;
        writer.WriteStartObject();

        // 序列化type（使用StringEnumConverter）
        writer.WritePropertyName("type");
        serializer.Serialize(writer, dataType.Type);

        // 序列化specs
        if (dataType.Specs != null)
        {
            writer.WritePropertyName("specs");
            serializer.Serialize(writer, dataType.Specs);
        }

        writer.WriteEndObject();
    }
}
