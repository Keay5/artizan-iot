using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

/// <summary>
/// 集合类型转换器（处理List<IInputParam>）
/// </summary>
public class InputParamListConverter : JsonConverter<List<IInputParam>>
{
    // 复用单个对象转换器，避免逻辑冗余
    private readonly InputParamConverter _itemConverter = new();

    /// <summary>
    /// 序列化集合：遍历每个元素并调用单个转换器
    /// </summary>
    public override void WriteJson(JsonWriter writer, List<IInputParam>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray(); // 写入 JSON 数组开始标记
        foreach (var item in value)
        {
            // 对每个元素调用单个转换器
            _itemConverter.WriteJson(writer, item, serializer);
        }
        writer.WriteEndArray(); // // 写入 JSON 数组结束标记
    }

    /// <summary>
    /// 反序列化集合：读取数组并逐个还原元素
    /// </summary>
    /// <returns></returns>
    public override List<IInputParam>? ReadJson(JsonReader reader, Type objectType, List<IInputParam>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var list = new List<IInputParam>();
        // 处理 reader 已在数组内部的情况
        if (reader.TokenType != JsonToken.StartArray)
        {
            // 尝试读取单个元素（兼容非数组场景）
            var item = _itemConverter.ReadJson(reader, typeof(IInputParam), null, false, serializer);
            if (item != null)
                list.Add(item);
            return list;
        }

        // 遍历数组元素
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray)
                break;

            // 对每个元素调用单个转换器反序列化
            var item = _itemConverter.ReadJson(reader, typeof(IInputParam), null, false, serializer);
            if (item != null)
                list.Add(item);
        }


        return list;
    }

}
