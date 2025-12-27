using Artizan.IoT.Things.Caches.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Caching;

namespace Artizan.IoT.Things.Caches.Managers;

/// <summary>
/// 历史属性数据缓存管理器实现（门面模式核心）
/// 设计思路：
/// 1. 实现IThingPropertyHistoryDataCacheManager接口，封装时序数据的存储操作
/// 2. 聚焦"时间范围查询、批量追加、过期清理"核心能力，适配IoT历史数据场景
/// 设计理念：
/// - 时序特性：将业务层的"时间范围"转换为存储层的"分数范围"（毫秒级时间戳），适配ZSet/SortedDictionary的排序特性
/// - 分页保护：限制最大分页大小，避免单次查询过多数据导致内存溢出
/// 设计考量：
/// - 快捷时间范围：封装"最近N分钟"查询逻辑，简化调用方代码
/// - 数据保留：自动清理超过保留时长的历史数据，避免存储溢出
/// </summary>
public class ThingPropertyHistoryDataCacheManager :
    ThingPropertyHistoryDataCacheManagerBase,
    IThingPropertyHistoryDataCacheManager
{
    private readonly IThingCacheStorageProvider _storageProvider;
    private readonly ThingPropertyHistoryDataCacheOptions _historyOptions;

    /// <summary>
    /// 构造函数（依赖注入）
    /// </summary>
    public ThingPropertyHistoryDataCacheManager(
        IThingCacheStorageProvider storageProvider,
        IOptions<ThingPropertyHistoryDataCacheOptions> historyOptions,
        IOptions<AbpDistributedCacheOptions> abpDistributedCacheOptions)
        : base(abpDistributedCacheOptions)
    {
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _historyOptions = historyOptions?.Value ?? throw new ArgumentNullException(nameof(historyOptions));
    }

    /// <summary>
    /// 新增一条历史属性数据
    /// </summary>
    public override async Task AddAsync(
        ThingPropertyDataCacheItem cacheItem,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        ValidateCacheItem(cacheItem);

        // 生成缓存键和分数（时间戳转毫秒级）
        var cacheKey = BuildQualifiedCacheKey(cacheItem.CalculateHistoryDataCacheKey());
        var score = cacheItem.ConvertTimeStampToScore();

        // 追加到时序存储
        await _storageProvider.AddZSetItemAsync(cacheKey, cacheItem, score, cancellationToken);
    }

    /// <summary>
    /// 批量新增历史属性数据
    /// </summary>
    public override async Task AddManyAsync(
        IEnumerable<ThingPropertyDataCacheItem> cacheItems,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        if (cacheItems == null)
        {
            throw new ArgumentNullException(nameof(cacheItems));
        }
        var cacheItemList = cacheItems.ToList();
        if (!cacheItemList.Any())
        {
            throw new ArgumentException("缓存项列表不能为空", nameof(cacheItems));
        }

        // 按缓存键分组（同一设备同一属性的历史数据归为一组）
        var groupedItems = cacheItemList
            .GroupBy(item => item.CalculateHistoryDataCacheKey())
            .ToList();

        // 逐组批量写入
        foreach (var group in groupedItems)
        {
            var cacheKey = BuildQualifiedCacheKey(group.Key);
            var valueScores = group.Select(item =>
            {
                ValidateCacheItem(item);
                return (Value: item, Score: item.ConvertTimeStampToScore());
            }).ToList();

            await _storageProvider.AddZSetItemsAsync(cacheKey, valueScores, cancellationToken);
        }
    }

    /// <summary>
    /// 按时间范围查询历史属性数据
    /// </summary>
    public override async Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByTimeRangeAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        DateTime startTime,
        DateTime endTime,
        int pageIndex = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        ValidateParameters(productKey, deviceName, propertyIdentifier);
        ValidateTimeRange(startTime, endTime);
        ValidatePageParams(ref pageIndex, ref pageSize);

        // 转换时间范围为分数范围（毫秒级时间戳）
        var minScore = new DateTimeOffset(startTime).ToUnixTimeMilliseconds();
        var maxScore = new DateTimeOffset(endTime).ToUnixTimeMilliseconds();

        // 生成缓存键并查询
        var cacheKey = BuildQualifiedCacheKey(ThingPropertyDataCacheItem.CalculateHistoryDataCacheKey(productKey, deviceName, propertyIdentifier));
        var result = await _storageProvider.GetZSetByScoreRangeAsync(
            cacheKey, minScore, maxScore, pageIndex, pageSize, cancellationToken);

        return result;
    }

    /// <summary>
    /// 按快捷时间范围（最近N分钟）查询历史属性数据
    /// </summary>
    public override async Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByRecentMinutesAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        int minutes,
        int pageIndex = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        // 参数校验（支持1/5/10/15分钟）
        var validMinutes = new[] { 1, 5, 10, 15 };
        if (!validMinutes.Contains(minutes))
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), $"仅支持{string.Join(",", validMinutes)}分钟的快捷查询");
        }
        ValidateParameters(productKey, deviceName, propertyIdentifier);
        ValidatePageParams(ref pageIndex, ref pageSize);

        // 计算时间范围（当前时间往前推N分钟）
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-minutes);

        // 复用时间范围查询逻辑
        return await GetByTimeRangeAsync(
            productKey, deviceName, propertyIdentifier,
            startTime, endTime,
            pageIndex, pageSize,
            cancellationToken);
    }

    /// <summary>
    /// 删除指定时间范围内的历史属性数据
    /// </summary>
    public override async Task RemoveByTimeRangeAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        ValidateParameters(productKey, deviceName, propertyIdentifier);
        ValidateTimeRange(startTime, endTime);

        // 转换时间范围为分数范围
        var minScore = new DateTimeOffset(startTime).ToUnixTimeMilliseconds();
        var maxScore = new DateTimeOffset(endTime).ToUnixTimeMilliseconds();

        // 生成缓存键并删除
        var cacheKey = BuildQualifiedCacheKey(ThingPropertyDataCacheItem.CalculateHistoryDataCacheKey(productKey, deviceName, propertyIdentifier));
        await _storageProvider.RemoveZSetByScoreRangeAsync(cacheKey, minScore, maxScore, cancellationToken);
    }

    /// <summary>
    /// 清理过期的历史数据
    /// </summary>
    public override async Task CleanupExpiredHistoryAsync(CancellationToken cancellationToken = default)
    {
        // 计算过期时间（当前时间 - 保留时长）
        var expireTime = DateTime.UtcNow.Subtract(_historyOptions.HistoryRetention);
        var maxScore = new DateTimeOffset(expireTime).ToUnixTimeMilliseconds();

        // 获取所有历史数据缓存键前缀
        var historyPrefix = BuildQualifiedCacheKey(ThingPropertyDataCacheItem.CalculateAllHistoryDataCacheKey());
        var keys = await _storageProvider.GetKeysByPrefixAsync(historyPrefix, cancellationToken);

        // 逐键清理过期数据
        foreach (var key in keys)
        {
            await _storageProvider.RemoveZSetByScoreRangeAsync(key, 0, maxScore, cancellationToken);
        }
    }

    #region 私有辅助方法
    /// <summary>
    /// 校验缓存项（必须包含完整的业务参数和时间戳）
    /// </summary>
    private void ValidateCacheItem(ThingPropertyDataCacheItem cacheItem)
    {
        if (cacheItem == null)
        {
            throw new ArgumentNullException(nameof(cacheItem));
        }
        ValidateParameters(cacheItem.ProductKey, cacheItem.DeviceName, cacheItem.PropertyIdentifier);

        // 时间戳不能是默认值（必须显式设置）
        if (cacheItem.TimeStamp == default)
        {
            throw new ArgumentException("历史数据缓存项必须设置有效的时间戳", nameof(cacheItem.TimeStamp));
        }
    }

    /// <summary>
    /// 校验时间范围（开始时间 <= 结束时间，且不能是未来时间）
    /// </summary>
    private void ValidateTimeRange(DateTime startTime, DateTime endTime)
    {
        if (startTime > endTime)
        {
            throw new ArgumentException("开始时间不能晚于结束时间", nameof(startTime));
        }
        if (endTime > DateTime.UtcNow)
        {
            throw new ArgumentException("结束时间不能晚于当前UTC时间", nameof(endTime));
        }
    }

    /// <summary>
    /// 校验分页参数（修正非法值，限制最大页大小）
    /// </summary>
    private void ValidatePageParams(ref int pageIndex, ref int pageSize)
    {
        // 页码最小为1
        if (pageIndex < 1)
        {
            pageIndex = 1;
        }

        // 页大小默认值和最大值限制
        if (pageSize < 1)
        {
            pageSize = _historyOptions.DefaultPageSize;
        }
        if (pageSize > _historyOptions.MaxPageSize)
        {
            pageSize = _historyOptions.MaxPageSize;
        }
    }

    /// <summary>
    /// 通用参数校验（产品Key+设备名称+属性标识）
    /// </summary>
    private void ValidateParameters(string productKey, string deviceName, string propertyIdentifier)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentException("产品标识不能为空或空白", nameof(productKey));
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("设备名称不能为空或空白", nameof(deviceName));
        }
        if (string.IsNullOrWhiteSpace(propertyIdentifier))
        {
            throw new ArgumentException("属性标识符不能为空或空白", nameof(propertyIdentifier));
        }
    }
    #endregion
}