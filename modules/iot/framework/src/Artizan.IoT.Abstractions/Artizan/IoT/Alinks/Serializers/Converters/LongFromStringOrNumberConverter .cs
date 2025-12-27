using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.Serializers.Converters;

/// <summary>
/// 支持将JSON字符串（如"123"）或数字（如123）转换为long的转换器
/// </summary>
public class LongFromStringOrNumberConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 情况1：JSON是字符串（如"123"）→ 解析为long
        if (reader.TokenType == JsonTokenType.String)
        {
            string? strValue = reader.GetString();
            if (long.TryParse(strValue, out long longValue))
            {
                return longValue;
            }
            throw new JsonException($"字符串\"{strValue}\"无法转换为long类型");
        }

        // 情况2：JSON是数字（如123）→ 直接转换为long
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        // 其他类型报错
        throw new JsonException($"不支持的id类型：{reader.TokenType}，需为字符串或数字");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // 序列化时按数字输出（符合多数场景需求）
        writer.WriteNumberValue(value);
    }
}
