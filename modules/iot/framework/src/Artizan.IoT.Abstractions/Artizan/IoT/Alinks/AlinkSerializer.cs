using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Artizan.IoT.Alinks;

/// <summary>
/// // TODO: 目前使用 Newtonsoft.Json 进行序列化和反序列化，后续考虑迁移到 System.Text.Json 以提升性能。或者提供接口让用户自行选择序列化库。
/// System.Text.Json:
/// 序列化
/// https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/how-to
/// 反序列化：
/// https://learn.microsoft.com/zh-cn/dotnet/standard/serialization/system-text-json/deserialization
/// </summary>
public static class AlinkSerializer
{
    public static readonly JsonSerializerSettings DefaulJsonSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None, // None 表示序列化后的 JSON 为紧凑格式（无缩进、无换行），适合网络传输或存储
        Converters = {
            new StringEnumConverter(), // 用于将枚举类型序列化为其字符串名称（而非默认的整数）
        }
        // 特别注意：这个千万不要启用，否则Json 中必要的节点因为是默认值而被忽略掉，导致Json Schema 校验检查失败。
        // DefaultValueHandling = DefaultValueHandling.Ignore, // 对于具有默认值的属性（如 int 类型的 0、bool 类型的 false 等），设置为 Ignore 时将不序列化这些属性
    };

    /// <summary>
    /// 序列化对象（通用方法）
    /// </summary>
    public static string SerializeObject(object value, JsonSerializerSettings? settings = null)
    {
        settings ??= DefaulJsonSerializerSettings;
        return JsonConvert.SerializeObject(value, settings);
    }

    /// <summary>
    /// 反序列化为指定类型
    /// </summary>
    public static object DeserializeObject(string json, Type targetType, JsonSerializerSettings? settings = null)
    {
        settings ??= DefaulJsonSerializerSettings;
        return JsonConvert.DeserializeObject(json, targetType, settings);
    }

    public static T? DeserializeObject<T>(string json, JsonSerializerSettings? settings = null)
    {
        settings ??= DefaulJsonSerializerSettings;

        try  // 添加异常处理，避免非法 JSON 导致程序崩溃：
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        catch (JsonException ex)
        {
            // 日志记录或返回null
            // TODO: Abp 日志，
            Console.WriteLine($"反序列化失败：{ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 反序列化为动态类型
    /// </summary>
    public static dynamic DeserializeDynamic(string json, JsonSerializerSettings? settings = null)
    {
        settings ??= DefaulJsonSerializerSettings;
        return JsonConvert.DeserializeObject<dynamic>(json, settings);
    }

}
