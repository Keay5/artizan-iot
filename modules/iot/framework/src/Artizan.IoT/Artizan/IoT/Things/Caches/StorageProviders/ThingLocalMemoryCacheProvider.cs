using Artizan.IoT.Things.Caches.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.StorageProviders;

/// <summary>
/// 本地内存缓存提供者（策略模式具体实现）
/// 设计思路：
/// 1. 基于ConcurrentDictionary（最新数据）和SortedDictionary（历史数据）实现线程安全的本地缓存
/// 2. 内置过期逻辑，支持绝对/滑动过期模式，适配高并发场景
/// 设计理念：
/// - 高性能：本地内存操作无网络开销，适配低延迟场景（开发/测试环境首选）
/// - 轻量级：无需依赖第三方组件，开箱即用
/// 设计考量：
/// - 线程安全：所有操作基于线程安全集合，避免锁竞争
/// - 过期清理：后台定时清理过期缓存，避免内存泄漏
/// - 时序存储：SortedDictionary按分数（时间戳）排序，适配历史数据的时间范围查询
/// </summary>
public class ThingLocalMemoryCacheProvider : IThingCacheStorageProvider
{
    #region 内部存储结构
    /// <summary>
    /// 最新数据缓存条目（封装值+过期元数据）
    /// </summary>
    private class LatestCacheEntry
    {
        public ThingPropertyDataCacheItem Value { get; set; } = default!;
        public CacheExpireMode ExpireMode { get; set; }
        public TimeSpan Expiration { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 历史数据缓存条目（时序存储，按分数排序）
    /// </summary>
    private class HistoryCacheEntry
    {
        // SortedDictionary：Key=分数（毫秒级时间戳），Value=缓存项（支持按分数范围查询）
        public SortedDictionary<double, ThingPropertyDataCacheItem> Items { get; } = new();
        // 写入锁：保证SortedDictionary的线程安全（SortedDictionary本身非线程安全）
        public readonly object LockObj = new();
    }
    #endregion

    // 最新数据存储：Key=缓存键，Value=LatestCacheEntry
    private readonly ConcurrentDictionary<string, LatestCacheEntry> _latestCache = new();
    // 历史数据存储：Key=缓存键，Value=HistoryCacheEntry
    private readonly ConcurrentDictionary<string, HistoryCacheEntry> _historyCache = new();
    // 清理锁：避免并发清理导致的线程安全问题
    private readonly object _cleanupLock = new();

    #region 最新数据操作（K-V存储）
    /// <summary>
    /// 获取单个最新数据缓存项
    /// </summary>
    public Task<ThingPropertyDataCacheItem?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 尝试获取缓存条目
        if (_latestCache.TryGetValue(key, out var entry))
        {
            // 检查是否过期（即使单行也用{}）
            if (IsExpired(entry))
            {
                _latestCache.TryRemove(key, out _);
                return Task.FromResult<ThingPropertyDataCacheItem?>(null);
            }

            // 滑动过期：更新最后访问时间和过期时间
            if (entry.ExpireMode == CacheExpireMode.Sliding)
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.ExpirationTime = entry.LastAccessTime + entry.Expiration;
            }

            return Task.FromResult<ThingPropertyDataCacheItem?>(entry.Value);
        }

        return Task.FromResult<ThingPropertyDataCacheItem?>(null);
    }

    /// <summary>
    /// 批量获取最新数据缓存项
    /// </summary>
    public Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new Dictionary<string, ThingPropertyDataCacheItem>();

        foreach (var key in keys)
        {
            // 即使单行也用{}，符合代码规范
            if (_latestCache.TryGetValue(key, out var entry))
            {
                if (IsExpired(entry))
                {
                    _latestCache.TryRemove(key, out _);
                }
                else
                {
                    if (entry.ExpireMode == CacheExpireMode.Sliding)
                    {
                        entry.LastAccessTime = DateTime.UtcNow;
                        entry.ExpirationTime = entry.LastAccessTime + entry.Expiration;
                    }
                    result[key] = entry.Value;
                }
            }
        }

        return Task.FromResult<IDictionary<string, ThingPropertyDataCacheItem>>(result);
    }

    /// <summary>
    /// 设置单个最新数据缓存项
    /// </summary>
    public Task SetAsync(
        string key,
        ThingPropertyDataCacheItem value,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 参数校验
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "过期时间必须大于0");
        }

        // 构建缓存条目
        var entry = new LatestCacheEntry
        {
            Value = value,
            ExpireMode = expireMode,
            Expiration = expiration.Value,
            ExpirationTime = expireMode == CacheExpireMode.Absolute
                ? DateTime.UtcNow + expiration.Value
                : DateTime.UtcNow + expiration.Value,
            LastAccessTime = DateTime.UtcNow
        };

        // 存入缓存（覆盖已有值）
        _latestCache[key] = entry;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量设置最新数据缓存项
    /// </summary>
    public Task SetManyAsync(
        IDictionary<string, ThingPropertyDataCacheItem> keyValues,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 参数校验
        if (keyValues == null || !keyValues.Any())
        {
            throw new ArgumentException("缓存键值对不能为空", nameof(keyValues));
        }
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "过期时间必须大于0");
        }

        // 批量写入（ConcurrentDictionary支持并发写入）
        foreach (var kvp in keyValues)
        {
            var entry = new LatestCacheEntry
            {
                Value = kvp.Value,
                ExpireMode = expireMode,
                Expiration = expiration.Value,
                ExpirationTime = expireMode == CacheExpireMode.Absolute
                    ? DateTime.UtcNow + expiration.Value
                    : DateTime.UtcNow + expiration.Value,
                LastAccessTime = DateTime.UtcNow
            };
            _latestCache[kvp.Key] = entry;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 移除单个最新数据缓存项
    /// </summary>
    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _latestCache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量移除最新数据缓存项
    /// </summary>
    public Task RemoveManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var key in keys)
        {
            _latestCache.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查最新数据缓存项是否存在
    /// </summary>
    public Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_latestCache.TryGetValue(key, out var entry))
        {
            if (IsExpired(entry))
            {
                _latestCache.TryRemove(key, out _);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// 根据前缀获取最新数据缓存键
    /// </summary>
    public Task<IEnumerable<string>> GetKeysByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var keys = _latestCache.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        return Task.FromResult<IEnumerable<string>>(keys);
    }
    #endregion

    #region 历史数据操作（时序存储）
    /// <summary>
    /// 新增一条历史数据缓存项
    /// </summary>
    public Task AddZSetItemAsync(
        string key,
        ThingPropertyDataCacheItem value,
        double score,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 获取或创建历史数据条目
        var historyEntry = _historyCache.GetOrAdd(key, _ => new HistoryCacheEntry());

        // 加锁保证SortedDictionary的线程安全
        lock (historyEntry.LockObj)
        {
            // 分数（时间戳）可能重复，追加后缀保证唯一性
            var uniqueScore = GetUniqueScore(historyEntry.Items, score);
            historyEntry.Items[uniqueScore] = value;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量新增历史数据缓存项
    /// </summary>
    public Task AddZSetItemsAsync(
        string key,
        IEnumerable<(ThingPropertyDataCacheItem Value, double Score)> valueScores,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (valueScores == null || !valueScores.Any())
        {
            throw new ArgumentException("缓存项列表不能为空", nameof(valueScores));
        }

        // 获取或创建历史数据条目
        var historyEntry = _historyCache.GetOrAdd(key, _ => new HistoryCacheEntry());

        // 加锁批量写入
        lock (historyEntry.LockObj)
        {
            foreach (var (value, score) in valueScores)
            {
                var uniqueScore = GetUniqueScore(historyEntry.Items, score);
                historyEntry.Items[uniqueScore] = value;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 按分数范围查询历史数据缓存项
    /// </summary>
    public Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new List<ThingPropertyDataCacheItem>();
        int totalCount = 0;

        // 检查缓存键是否存在
        if (_historyCache.TryGetValue(key, out var historyEntry))
        {
            lock (historyEntry.LockObj)
            {
                // 筛选分数范围内的项
                var filteredItems = historyEntry.Items
                    .Where(kvp => kvp.Key >= minScore && kvp.Key <= maxScore)
                    .ToList();

                totalCount = filteredItems.Count;

                // 分页处理
                if (totalCount > 0)
                {
                    var skip = (pageIndex - 1) * pageSize;
                    result = filteredItems
                        .Skip(skip)
                        .Take(pageSize)
                        .Select(kvp => kvp.Value)
                        .ToList();
                }
            }
        }

        return Task.FromResult((Items: (IList<ThingPropertyDataCacheItem>)result, TotalCount: totalCount));
    }

    /// <summary>
    /// 删除指定分数范围的历史数据缓存项
    /// </summary>
    public Task RemoveZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_historyCache.TryGetValue(key, out var historyEntry))
        {
            lock (historyEntry.LockObj)
            {
                // 筛选需要删除的分数
                var scoresToRemove = historyEntry.Items
                    .Where(kvp => kvp.Key >= minScore && kvp.Key <= maxScore)
                    .Select(kvp => kvp.Key)
                    .ToList();

                // 批量删除
                foreach (var score in scoresToRemove)
                {
                    historyEntry.Items.Remove(score);
                }

                // 如果为空，移除整个条目（节省内存）
                if (!historyEntry.Items.Any())
                {
                    _historyCache.TryRemove(key, out _);
                }
            }
        }

        return Task.CompletedTask;
    }
    #endregion

    /// <summary>
    /// 清理过期缓存项（最新数据）
    /// </summary>
    public Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 加锁避免并发清理
        lock (_cleanupLock)
        {
            // 清理过期的最新数据
            var expiredLatestKeys = _latestCache.Where(kvp => IsExpired(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredLatestKeys)
            {
                _latestCache.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    #region 私有辅助方法
    /// <summary>
    /// 检查最新数据缓存条目是否过期
    /// </summary>
    private bool IsExpired(LatestCacheEntry entry)
    {
        return DateTime.UtcNow > entry.ExpirationTime;
    }

    /// <summary>
    /// 获取唯一分数（避免时间戳重复导致的键冲突）
    /// </summary>
    private double GetUniqueScore(SortedDictionary<double, ThingPropertyDataCacheItem> items, double score)
    {
        var uniqueScore = score;
        int suffix = 1;

        // 如果分数已存在，追加0.000x后缀（不影响时间戳的毫秒级精度）
        while (items.ContainsKey(uniqueScore))
        {
            uniqueScore = score + (suffix * 0.0001);
            suffix++;
        }

        return uniqueScore;
    }
    #endregion
}