using System;

namespace Artizan.IoTHub.Things.Caches;


/// <summary>
/// 缓存数据实体类（标准化设备属性缓存结构）
/// 设计理念：统一缓存数据格式，使缓存具备自描述性，适配微服务多服务间共享缓存场景
/// 设计考量：
/// 1. 强制包含ProductKey和DeviceName，确保设备唯一性，避免不同产品/设备缓存键冲突
/// 2. 采用UTC毫秒级时间戳（TimestampMs），解决跨时区时间同步及序列化/反序列化时区偏差问题
/// 3. 提供静态缓存键生成方法，统一命名规范，降低维护成本并便于批量缓存操作
/// 4. 标记[Serializable]，支持序列化场景（分布式缓存传输、日志持久化等）
/// 【设计考量】
/// 1. 数据标准化：统一设备属性缓存的格式，便于读取和解析
/// 2. 缓存键生成：静态方法CalculateCacheKey，确保键规则全局统一
/// 3. 时间戳：毫秒级UTC时间戳，便于排序和过滤超期数据
/// </summary>
[Serializable]
public class ThingPropertyDataCacheItem
{
    /// <summary>
    /// 产品密钥（设备唯一标识组成部分，与DeviceName联合唯一）
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（设备唯一标识组成部分，与ProductKey联合唯一）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 设备属性数据（动态结构，适配不同型号设备的属性差异）
    /// 属性数据（JSON格式/字典格式，按需调整）
    /// </summary>
    public object Data { get; set; } = new object();

    /// <summary>
    /// 数据产生时间戳（UTC，毫秒级）
    /// 设计考量：避免DateTime类型序列化后的时区问题，统一时间标准
    /// </summary>
    public long TimestampUtcMs { get; set; }

    /// <summary>
    /// 构造函数（强制初始化核心字段，避免无效实例）
    /// 设计思路：通过构造函数约束必填字段，确保缓存项的完整性
    /// 静态方法：避免多处定义键规则导致不一致
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="data">设备属性数据</param>
    /// <param name="timestampUtcMs">时间戳,UTC毫秒级</param>
    public ThingPropertyDataCacheItem(string productKey, string deviceName, object data, long timestampUtcMs)
    {
        ProductKey = productKey;
        DeviceName = deviceName;
        Data = data;
        // 本地时间转UTC毫秒级时间戳，统一时间基准
        TimestampUtcMs = timestampUtcMs;
    }

    /// <summary>
    /// 生成缓存键（统一命名规范）
    /// 设计思路：采用"pk:产品键dn:设备名:业务标识"格式，兼具唯一性、可读性和可扩展性
    /// 优势：便于缓存管理、问题排查，支持按产品/设备维度批量操作缓存
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <returns>设备属性缓存的唯一键</returns>
    public static string CalculateCacheKey(string productKey, string deviceName)
    {
        //return $"pk:{productKey}dn:{deviceName}:ThingPropertyData";
        return $"ThingProperty:pk:{productKey}:dn:{deviceName}";
    }

    public static string CalculateCacheHistoryKey(string productKey, string deviceName)
    {
        return $"ThingProperty:History:pk:{productKey}:dn:{deviceName}";
    }
}
