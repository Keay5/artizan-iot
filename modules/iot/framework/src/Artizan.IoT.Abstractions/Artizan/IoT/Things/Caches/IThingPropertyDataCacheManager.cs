using Artizan.IoT.Things.Caches.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches;

/// <summary>
/// 设备最新属性数据缓存管理器接口
/// 设计模式：门面模式（Facade Pattern）
/// 设计思路：
/// 1. 对外提供统一的最新属性数据缓存操作入口，屏蔽底层存储实现的复杂度
/// 2. 聚焦IoT场景"最新属性值"的核心诉求（高频读写、单值存储），简化调用方逻辑
/// 设计理念：
/// - 最少知识原则：调用方仅需关注业务参数（产品Key/设备名称/属性标识），无需关心缓存键生成、存储类型等技术细节
/// - 单一职责：仅封装"最新属性数据"的缓存操作，不掺杂历史数据逻辑
/// 设计考量：
/// - 批量操作支持：IoT设备常批量上报属性，批量读写可减少IO往返，提升吞吐量
/// - 异步优先：所有方法异步化，适配高并发的设备消息处理场景
/// </summary>
public interface IThingPropertyDataCacheManager
{
    /// <summary>
    /// 获取单个设备属性的最新缓存值
    /// </summary>
    /// <param name="productKey">产品标识（IoT设备唯一归属标识）</param>
    /// <param name="deviceName">设备名称（产品内设备唯一标识）</param>
    /// <param name="propertyIdentifier">属性标识符（与ThingModels中Property.Identifier对齐）</param>
    /// <param name="cancellationToken">取消令牌（适配服务优雅关闭）</param>
    /// <returns>缓存项（未命中/过期返回null）</returns>
    Task<ThingPropertyDataCacheItem?> GetAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取设备属性的最新缓存值（核心优化：减少IO次数）
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifiers">属性标识符列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键值对字典（键=属性标识符，值=缓存项；仅返回命中的缓存项）</returns>
    Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(
        string productKey,
        string deviceName,
        IEnumerable<string> propertyIdentifiers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置单个设备属性的最新缓存值
    /// </summary>
    /// <param name="cacheItem">缓存项实体（包含完整属性元数据）</param>
    /// <param name="expireMode">过期模式（绝对/滑动）</param>
    /// <param name="expiration">过期时间（null则使用全局默认配置）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetAsync(
        ThingPropertyDataCacheItem cacheItem,
        CacheExpireMode expireMode = CacheExpireMode.Absolute,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量设置设备属性的最新缓存值（适配设备批量上报场景）
    /// </summary>
    /// <param name="cacheItems">缓存项列表</param>
    /// <param name="expireMode">过期模式</param>
    /// <param name="expiration">过期时间（null则使用全局默认配置）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetManyAsync(
        IEnumerable<ThingPropertyDataCacheItem> cacheItems,
        CacheExpireMode expireMode = CacheExpireMode.Absolute,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除单个设备属性的缓存
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量移除设备属性的缓存
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifiers">属性标识符列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveManyAsync(
        string productKey,
        string deviceName,
        IEnumerable<string> propertyIdentifiers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除指定设备的所有属性缓存（批量操作）
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveDeviceAllPropertiesAsync(
        string productKey,
        string deviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查属性缓存是否存在（非侵入式检查，不更新过期时间）
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default);
}