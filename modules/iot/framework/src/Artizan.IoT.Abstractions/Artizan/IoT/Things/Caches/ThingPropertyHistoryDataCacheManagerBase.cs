using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Caching;

namespace Artizan.IoT.Things.Caches;

public abstract class ThingPropertyHistoryDataCacheManagerBase : IThingPropertyHistoryDataCacheManager
{
    /// <summary>
    /// ABP分布式缓存配置（复用原有配置）
    /// </summary>
    protected readonly AbpDistributedCacheOptions _abpDistributedCacheOptions;

    /// <summary>
    /// 项目专属前缀（可抽离到配置，此处硬编码为示例）
    /// </summary>
    protected readonly string _projectPrefix;

    /// <summary>
    /// 构造函数：注入ABP缓存配置
    /// </summary>
    public ThingPropertyHistoryDataCacheManagerBase(IOptions<AbpDistributedCacheOptions> abpDistributedCacheOptions)
    {
        _abpDistributedCacheOptions = abpDistributedCacheOptions.Value;
    }

    /// <summary>
    /// 构建 “合格 / 规范化” 的缓存键:避免应用程序缓存相互污染
    /// 统一生成带前缀的缓存Key（子类复用）
    /// </summary>
    /// <param name="originalKey">原始业务Key</param>
    /// <returns>完整缓存Key（ABP前缀 + 项目前缀 + 原始Key）</returns>
    protected virtual string BuildQualifiedCacheKey(string originalKey)
    {
        if (string.IsNullOrWhiteSpace(originalKey))
        {
            throw new ArgumentNullException(nameof(originalKey), "缓存原始Key不能为空");
        }

        //注意：不加入_projectPrefix，避免微服务架构下，微服务间无法共享缓存
        var appPrefix = !string.IsNullOrWhiteSpace(_abpDistributedCacheOptions.KeyPrefix)
            ? $"{_abpDistributedCacheOptions.KeyPrefix}:"
            : "";
        return $"{appPrefix}{originalKey}";
    }

    public abstract Task AddAsync(ThingPropertyDataCacheItem cacheItem, CancellationToken cancellationToken = default);
    public abstract Task AddManyAsync(IEnumerable<ThingPropertyDataCacheItem> cacheItems, CancellationToken cancellationToken = default);
    public abstract Task CleanupExpiredHistoryAsync(CancellationToken cancellationToken = default);
    public abstract Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByRecentMinutesAsync(string productKey, string deviceName, string propertyIdentifier, int minutes, int pageIndex = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    public abstract Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByTimeRangeAsync(string productKey, string deviceName, string propertyIdentifier, DateTime startTime, DateTime endTime, int pageIndex = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    public abstract Task RemoveByTimeRangeAsync(string productKey, string deviceName, string propertyIdentifier, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
