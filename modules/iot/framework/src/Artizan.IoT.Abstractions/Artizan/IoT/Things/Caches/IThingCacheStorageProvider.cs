using Artizan.IoT.Things.Caches;
using Artizan.IoT.Things.Caches.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches;

/// <summary>
/// 设备缓存存储提供者接口
/// 设计模式：策略模式（Strategy Pattern）
/// 设计思路：
/// 1. 定义缓存存储的通用操作契约，隔离存储实现与业务逻辑
/// 2. 支持不同存储介质（本地内存/Redis）的无缝切换，无需修改上层业务代码
/// 设计理念：
/// - 接口隔离原则：仅暴露存储层必要操作，不包含业务语义（如产品/设备维度）
/// - 开闭原则：新增存储类型（如MongoDB）仅需实现此接口，无需修改现有代码
/// 设计考量：
/// - 时序数据支持：补充ZSet相关操作（Redis）/SortedDictionary（内存），适配历史数据的时间排序需求
/// - 批量操作：减少高并发场景下的IO次数，提升性能
/// </summary>
public interface IThingCacheStorageProvider
{
    #region 最新数据操作（K-V存储）
    /// <summary>
    /// 获取单个K-V缓存项
    /// </summary>
    Task<ThingPropertyDataCacheItem?> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取K-V缓存项
    /// </summary>
    Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置单个K-V缓存项
    /// </summary>
    Task SetAsync(
        string key,
        ThingPropertyDataCacheItem value,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量设置K-V缓存项
    /// </summary>
    Task SetManyAsync(
        IDictionary<string, ThingPropertyDataCacheItem> keyValues,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除单个K-V缓存项
    /// </summary>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量移除K-V缓存项
    /// </summary>
    Task RemoveManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查K-V缓存项是否存在
    /// </summary>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据前缀获取所有匹配的缓存键
    /// </summary>
    Task<IEnumerable<string>> GetKeysByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default);
    #endregion

    #region 历史数据操作（时序存储）
    /// <summary>
    /// 新增一条时序缓存项（追加模式）
    /// </summary>
    Task AddZSetItemAsync(
        string key,
        ThingPropertyDataCacheItem value,
        double score, // 时间戳（毫秒级），用于排序
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量新增时序缓存项
    /// </summary>
    Task AddZSetItemsAsync(
        string key,
        IEnumerable<(ThingPropertyDataCacheItem Value, double Score)> valueScores,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按分数范围（时间戳）查询时序缓存项
    /// </summary>
    Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定分数范围的时序缓存项
    /// </summary>
    Task RemoveZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        CancellationToken cancellationToken = default);
    #endregion

    /// <summary>
    /// 清理过期缓存项（仅本地存储需要，Redis依赖内置机制）
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}