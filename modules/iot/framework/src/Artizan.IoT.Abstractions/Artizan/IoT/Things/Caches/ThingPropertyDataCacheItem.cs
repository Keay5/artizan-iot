using System;

namespace Artizan.IoT.Things.Caches;

/// <summary>
/// 设备属性数据缓存项（最新/历史数据通用模型）
/// 设计思路：
/// 1. 聚合设备属性的"业务数据"与"缓存元数据"，形成独立的缓存单元
/// 2. 兼容最新/历史数据场景：最新数据复用TimeStamp存储最后更新时间，历史数据存储上报时间
/// 设计理念：
/// - 命名对齐：PropertyIdentifier与ThingModels.Tsls.MetaDatas.Properties.Identifier保持一致，降低理解成本
/// - 元数据完备：包含时间戳、版本号、数据类型等，支持缓存审计、类型校验
/// 设计考量：
/// - 时间戳标准化：统一使用UTC时间，避免时区偏移导致的查询错误
/// - 版本控制：乐观锁机制，避免并发更新覆盖（仅最新数据生效）
/// </summary>
public class ThingPropertyDataCacheItem
{
    /// <summary>
    /// 产品标识（IoT设备归属的产品唯一标识）
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（产品内设备的唯一标识）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 属性标识符（与ThingModels中Property.Identifier严格对齐）
    /// </summary>
    public string PropertyIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 属性值（支持多类型，保留原始数据类型）
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 数据类型（如int/float/string/bool，用于反序列化校验）
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳（UTC）：
    /// - 最新数据：最后更新时间
    /// - 历史数据：上报时间
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 版本号（乐观锁：每次更新自增，避免并发覆盖，仅最新数据生效）
    /// </summary>
    public long Version { get; set; } = 1;

    #region 缓存键生成（核心：保证全局唯一性）
    /// <summary>
    /// 生成最新数据的全局唯一缓存键
    /// 键格式：thing:cache:latest:pk:{产品Key}:dn:{设备名称}:pid:{属性标识}
    /// 设计考量：分层前缀，支持按产品/设备维度批量操作
    /// </summary>
    /// <returns>最新数据缓存键</returns>
    public string CalculateLatestDataCacheKey()
    {
        return CalculateLatestDataCacheKey(ProductKey, DeviceName, PropertyIdentifier);
    }

    /// <summary>
    /// 静态方法：生成最新数据缓存键（适配批量操作场景）
    /// </summary>
    public static string CalculateLatestDataCacheKey(string productKey, string deviceName, string propertyIdentifier)
    {
        return $"thing:cache:latest:pk:{productKey}:dn:{deviceName}:pid:{propertyIdentifier}";
    }

    /// <summary>
    /// 生成历史数据的全局唯一缓存键
    /// 键格式：thing:cache:history:pk:{产品Key}:dn:{设备名称}:pid:{属性标识}
    /// 设计考量：与最新数据键前缀区分，避免冲突
    /// </summary>
    /// <returns>历史数据缓存键</returns>
    public string CalculateHistoryDataCacheKey()
    {
        return CalculateHistoryDataCacheKey(ProductKey, DeviceName, PropertyIdentifier);
    }

    /// <summary>
    /// 静态方法：生成历史数据缓存键
    /// </summary>
    public static string CalculateHistoryDataCacheKey(string productKey, string deviceName, string propertyIdentifier)
    {
        return $"thing:cache:history:pk:{productKey}:dn:{deviceName}:pid:{propertyIdentifier}";
    }

    /// <summary>
    /// 生成设备维度的最新数据缓存前缀（用于批量删除设备所有最新属性）
    /// </summary>
    public static string CalculateDeviceLatestDataPrefix(string productKey, string deviceName)
    {
        return $"thing:cache:latest:pk:{productKey}:dn:{deviceName}:pid:";
    }

    /// <summary>
    /// 生成设备维度的历史数据缓存前缀（用于批量清理设备所有历史属性）
    /// </summary>
    public static string CalculateDeviceHistoryDataPrefix(string productKey, string deviceName)
    {
        return $"thing:cache:history:pk:{productKey}:dn:{deviceName}:pid:";
    }
    #endregion

    /// <summary>
    /// 时间戳转换为毫秒级Score（适配Redis ZSet/内存SortedDictionary的排序需求）
    /// </summary>
    /// <returns>毫秒级时间戳</returns>
    public double ConvertTimeStampToScore()
    {
        return new DateTimeOffset(TimeStamp).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 毫秒级Score转换为UTC时间戳
    /// </summary>
    /// <param name="score">毫秒级时间戳</param>
    /// <returns>UTC时间</returns>
    public static DateTime ConvertScoreToTimeStamp(double score)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds((long)score).UtcDateTime;
    }
}