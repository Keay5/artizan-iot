using Artizan.IoTub.Things.Caches;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Things.Caches;

/// <summary>
/// 设备属性缓存查询服务实现（基于ABP分布式缓存）
/// 设计理念：
/// 1. 依赖注入解耦：通过构造函数注入缓存组件，避免硬编码依赖，便于替换缓存实现
/// 2. 缓存防护设计：实现缓存击穿防护策略，提升服务稳定性
/// 3. 性能优化：复用ABP提供的批量缓存API，减少网络交互开销
/// 核心职责：
/// - 封装缓存查询的技术细节（如缓存键生成、序列化/反序列化）
/// - 提供可靠的缓存查询能力，屏蔽底层缓存介质的复杂性
/// </summary>
public class ThingPropertyCacheQueryer : IThingPropertyCacheQueryer, ITransientDependency
{
    /// <summary>
    /// 最新属性数据分布式缓存（ABP泛型缓存组件）
    /// 设计考量：
    /// - 利用ABP的IDistributedCache<T>自动处理对象序列化/反序列化（默认使用JSON）
    /// - 泛型类型与缓存数据实体强绑定，避免类型转换错误
    /// </summary>
    private readonly IDistributedCache<ThingPropertyDataCacheItem> _propertyLatestCache;

    /// <summary>
    /// 历史属性数据分布式缓存（ABP泛型缓存组件）
    /// 设计考量：
    /// - 缓存值为List<ThingPropertyDataCacheItem>，直接对应历史数据集合
    /// - 与最新值缓存分离，避免数据结构相互影响
    /// </summary>
    private readonly IDistributedCache<List<ThingPropertyDataCacheItem>> _propertyHistoryCache;

    /// <summary>
    /// 构造函数（依赖注入）
    /// 设计模式：依赖注入模式（DI）
    /// 设计考量：
    /// - 明确声明依赖项，由ABP容器自动注入，符合"控制反转"原则
    /// - 仅注入必要的缓存组件，遵循"最小知识原则"
    /// </summary>
    /// <param name="propertyLatestCache">最新值分布式缓存</param>
    /// <param name="propertyHistoryCache">历史数据分布式缓存</param>
    public ThingPropertyCacheQueryer(
        IDistributedCache<ThingPropertyDataCacheItem> propertyLatestCache,
        IDistributedCache<List<ThingPropertyDataCacheItem>> propertyHistoryCache)
    {
        _propertyLatestCache = propertyLatestCache;
        _propertyHistoryCache = propertyHistoryCache;
    }

    /// <summary>
    /// 单设备查询最新属性值（含缓存击穿防护）
    /// 设计思路：
    /// 1. 生成标准缓存键（复用缓存实体的静态方法，保证键格式一致）
    /// 2. 查询缓存，若存在则直接返回
    /// 3. 若不存在，写入空值缓存（短期过期），避免缓存击穿（大量请求穿透至数据源）
    /// 缓存击穿防护原理：
    /// - 当缓存未命中时，主动写入一个空值并设置短期过期（5分钟）
    /// - 后续请求会命中空值缓存，避免同时穿透到底层存储
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新属性缓存项（null表示无数据）</returns>
    public async Task<ThingPropertyDataCacheItem?> GetLatestPropertyAsync(
        string productKey,
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        // 生成标准缓存键（与写入逻辑保持一致，避免键不匹配）
        var cacheKey = ThingPropertyDataCacheItem.CalculateCacheKey(productKey, deviceName);

        // 查询缓存（ABP封装的GetAsync方法，自动处理序列化）
        var cacheItem = await _propertyLatestCache.GetAsync(cacheKey, token: cancellationToken);

        // 缓存未命中时，写入空值缓存（防护逻辑）
        if (cacheItem == null)
        {
            // 空值缓存设置5分钟过期（平衡防护效果与数据实时性）
            await _propertyLatestCache.SetAsync(
                key: cacheKey,
                value: null, // 显式存入null
                options: new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                token: cancellationToken);
        }

        return cacheItem;
    }

    /// <summary>
    /// 单设备查询指定时长内的历史属性数据
    /// 设计思路：
    /// 1. 生成历史数据缓存键（基于最新值键扩展，保持命名关联性）
    /// 2. 查询缓存中的历史数据集合（无数据则返回空列表）
    /// 3. 按时间范围过滤数据（即使缓存中存在超期数据，也确保返回符合要求的结果）
    /// 设计考量：
    /// - 双重过滤：缓存写入时已过滤超期数据，查询时再次过滤，避免因缓存未及时更新导致的脏数据
    /// - 时间戳计算：使用UTC毫秒级时间戳，与缓存实体的时间标准保持一致，避免时区偏差
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="retainMinutes">保留分钟数（查询近N分钟数据）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>过滤后的历史属性数据列表（按时间升序排列）</returns>
    public async Task<List<ThingPropertyDataCacheItem>> GetHistoryPropertyAsync(
        string productKey,
        string deviceName,
        int retainMinutes,
        CancellationToken cancellationToken = default)
    {
        // 生成历史数据缓存键（命名规范：最新值的键与写入逻辑保持一致）
        var historyCacheKey = ThingPropertyDataCacheItem.CalculateCacheHistoryKey(productKey, deviceName);

        // 查询缓存（无数据则返回空列表，避免调用方处理null）
        var historyData = await _propertyHistoryCache.GetAsync(historyCacheKey, token: cancellationToken) ?? new List<ThingPropertyDataCacheItem>();

        // 计算时间阈值（当前UTC时间 - 保留分钟数，转换为毫秒级时间戳）
        var timeThreshold = DateTimeOffset.UtcNow.AddMinutes(-retainMinutes).ToUnixTimeMilliseconds();

        // 过滤超期数据并按时间升序排列（确保结果有序性）
        var filteredData = historyData
            .Where(item => item.TimestampUtcMs >= timeThreshold)
            .OrderBy(item => item.TimestampUtcMs)
            .ToList();

        return filteredData;
    }

    /// <summary>
    /// 批量查询多设备最新属性值（基于ABP批量API优化）
    /// 设计思路：
    /// 1. 批量生成缓存键（保证与单设备查询的键格式一致）
    /// 2. 调用ABP的GetManyAsync批量查询（一次网络请求获取所有数据）
    /// 3. 构建结果字典（键为缓存键，值为对应的缓存项）
    /// 性能优势：
    /// - 减少网络往返次数：N个设备查询从N次Redis请求减少为1次
    /// - 降低IO开销：批量操作比多次单条操作更节省Redis服务器资源
    /// 容错设计：
    /// - 即使部分设备无缓存数据，也会在字典中保留键并对应null值，保证结果完整性
    /// </summary>
    /// <param name="deviceList">设备列表（产品密钥+设备名称）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键：缓存键，值：最新属性缓存项（null表示无数据）</returns>
    public async Task<Dictionary<string, ThingPropertyDataCacheItem?>> GetLatestPropertiesBatchAsync(
        List<(string ProductKey, string DeviceName)> deviceList,
        CancellationToken cancellationToken = default)
    {
        // 入参校验（避免空列表导致的无效查询）
        if (deviceList == null || deviceList.Count == 0)
        {
            return new Dictionary<string, ThingPropertyDataCacheItem?>();
        }

        // 批量生成缓存键（复用静态方法，确保与写入逻辑一致）
        var cacheKeys = deviceList
            .Select(d => ThingPropertyDataCacheItem.CalculateCacheKey(d.ProductKey, d.DeviceName))
            .ToList();

        // ABP批量查询API（核心优化点：一次请求获取所有数据）
        var batchResult = await _propertyLatestCache.GetManyAsync(cacheKeys, token: cancellationToken);

        // 构建结果字典（直接复用批量查询的返回字典，其键值已对应）
        return batchResult.ToDictionary();
    }
}
