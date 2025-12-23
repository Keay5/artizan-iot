using Artizan.IoT.Things.Caches.Enums;
using Artizan.IoT.Things.Caches.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.Managers;

/// <summary>
/// 最新属性数据缓存管理器实现（门面模式核心）
/// 设计思路：
/// 1. 实现IThingPropertyDataCacheManager接口，封装底层存储提供者的调用逻辑
/// 2. 统一处理参数校验、缓存键生成、配置解析等通用逻辑
/// 设计理念：
/// - 封装变化：上层业务无需感知存储类型切换，由管理器适配不同存储提供者
/// - 防御式编程：前置参数校验，提前暴露错误，避免底层存储接收到无效参数
/// 设计考量：
/// - 配置解析：优先使用方法传入的过期配置，无则使用全局默认，保证灵活性
/// - 版本自增：每次Set操作自动更新版本号，实现乐观锁控制
/// - 批量操作优化：将业务维度的批量操作转换为存储层的批量操作，减少IO次数
/// </summary>
public class ThingPropertyDataCacheManager : IThingPropertyDataCacheManager
{
    private readonly IThingCacheStorageProvider _storageProvider;
    private readonly ThingPropertyDataCacheOptions _cacheOptions;

    /// <summary>
    /// 构造函数（依赖注入）
    /// </summary>
    /// <param name="storageProvider">存储提供者（策略模式注入）</param>
    /// <param name="cacheOptions">缓存配置（IOptions注入，支持动态更新）</param>
    /// <exception cref="ArgumentNullException">依赖为空时抛出</exception>
    public ThingPropertyDataCacheManager(
        IThingCacheStorageProvider storageProvider,
        IOptions<ThingPropertyDataCacheOptions> cacheOptions)
    {
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
    }

    /// <summary>
    /// 获取单个设备属性的最新缓存值
    /// </summary>
    public async Task<ThingPropertyDataCacheItem?> GetAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default)
    {
        // 参数校验（即使单行也用{}，符合代码规范）
        ValidateParameters(productKey, deviceName, propertyIdentifier);

        var cacheKey = ThingPropertyDataCacheItem.CalculateLatestDataCacheKey(productKey, deviceName, propertyIdentifier);
        return await _storageProvider.GetAsync(cacheKey, cancellationToken);
    }

    /// <summary>
    /// 批量获取设备属性的最新缓存值
    /// </summary>
    public async Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(
        string productKey,
        string deviceName,
        IEnumerable<string> propertyIdentifiers,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        ValidateParameters(productKey, deviceName);
        if (propertyIdentifiers == null || !propertyIdentifiers.Any())
        {
            throw new ArgumentException("属性标识符列表不能为空", nameof(propertyIdentifiers));
        }

        // 生成缓存键字典（键=属性标识，值=缓存键）
        var keyMap = propertyIdentifiers.ToDictionary(
            pid => pid,
            pid => ThingPropertyDataCacheItem.CalculateLatestDataCacheKey(productKey, deviceName, pid));

        // 批量获取缓存项
        var cacheItems = await _storageProvider.GetManyAsync(keyMap.Values, cancellationToken);

        // 转换为"属性标识-缓存项"字典（保持输入顺序）
        var result = new Dictionary<string, ThingPropertyDataCacheItem>();
        foreach (var pid in propertyIdentifiers)
        {
            if (cacheItems.TryGetValue(keyMap[pid], out var item))
            {
                result[pid] = item;
            }
        }

        return result;
    }

    /// <summary>
    /// 设置单个设备属性的最新缓存值
    /// </summary>
    public async Task SetAsync(
        ThingPropertyDataCacheItem cacheItem,
        CacheExpireMode expireMode = CacheExpireMode.Absolute,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // 参数校验
        if (cacheItem == null)
        {
            throw new ArgumentNullException(nameof(cacheItem));
        }
        ValidateParameters(cacheItem.ProductKey, cacheItem.DeviceName, cacheItem.PropertyIdentifier);

        // 自动更新元数据（版本+时间戳）
        cacheItem.TimeStamp = DateTime.UtcNow;
        cacheItem.Version++;

        // 解析有效过期配置（方法传入 > 全局默认）
        var effectiveExpiration = expiration ?? _cacheOptions.DefaultExpiration;
        var effectiveExpireMode = expireMode;
        if (effectiveExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "过期时间必须大于0");
        }

        // 生成缓存键并调用存储提供者
        var cacheKey = cacheItem.CalculateLatestDataCacheKey();
        await _storageProvider.SetAsync(
            cacheKey,
            cacheItem,
            effectiveExpireMode,
            effectiveExpiration,
            cancellationToken);
    }

    /// <summary>
    /// 批量设置设备属性的最新缓存值
    /// </summary>
    public async Task SetManyAsync(
        IEnumerable<ThingPropertyDataCacheItem> cacheItems,
        CacheExpireMode expireMode = CacheExpireMode.Absolute,
        TimeSpan? expiration = null,
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

        // 统一解析过期配置
        var effectiveExpiration = expiration ?? _cacheOptions.DefaultExpiration;
        var effectiveExpireMode = expireMode;
        if (effectiveExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "过期时间必须大于0");
        }

        // 构建缓存键-值字典
        var keyValues = new Dictionary<string, ThingPropertyDataCacheItem>();
        foreach (var cacheItem in cacheItemList)
        {
            ValidateParameters(cacheItem.ProductKey, cacheItem.DeviceName, cacheItem.PropertyIdentifier);

            // 自动更新元数据
            cacheItem.TimeStamp = DateTime.UtcNow;
            cacheItem.Version++;

            var cacheKey = cacheItem.CalculateLatestDataCacheKey();
            keyValues[cacheKey] = cacheItem;
        }

        // 批量写入存储
        await _storageProvider.SetManyAsync(keyValues, effectiveExpireMode, effectiveExpiration, cancellationToken);
    }

    /// <summary>
    /// 移除单个设备属性的缓存
    /// </summary>
    public async Task RemoveAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(productKey, deviceName, propertyIdentifier);

        var cacheKey = ThingPropertyDataCacheItem.CalculateLatestDataCacheKey(productKey, deviceName, propertyIdentifier);
        await _storageProvider.RemoveAsync(cacheKey, cancellationToken);
    }

    /// <summary>
    /// 批量移除设备属性的缓存
    /// </summary>
    public async Task RemoveManyAsync(
        string productKey,
        string deviceName,
        IEnumerable<string> propertyIdentifiers,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(productKey, deviceName);
        if (propertyIdentifiers == null || !propertyIdentifiers.Any())
        {
            throw new ArgumentException("属性标识符列表不能为空", nameof(propertyIdentifiers));
        }

        // 生成缓存键列表
        var cacheKeys = propertyIdentifiers
            .Select(pid => ThingPropertyDataCacheItem.CalculateLatestDataCacheKey(productKey, deviceName, pid))
            .ToList();

        // 批量移除
        await _storageProvider.RemoveManyAsync(cacheKeys, cancellationToken);
    }

    /// <summary>
    /// 移除指定设备的所有属性缓存
    /// </summary>
    public async Task RemoveDeviceAllPropertiesAsync(
        string productKey,
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(productKey, deviceName);

        // 获取设备维度缓存前缀，批量删除
        var prefix = ThingPropertyDataCacheItem.CalculateDeviceLatestDataPrefix(productKey, deviceName);
        var keys = await _storageProvider.GetKeysByPrefixAsync(prefix, cancellationToken);

        if (keys.Any())
        {
            await _storageProvider.RemoveManyAsync(keys, cancellationToken);
        }
    }

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(productKey, deviceName, propertyIdentifier);

        var cacheKey = ThingPropertyDataCacheItem.CalculateLatestDataCacheKey(productKey, deviceName, propertyIdentifier);
        return await _storageProvider.ExistsAsync(cacheKey, cancellationToken);
    }

    #region 私有辅助方法
    /// <summary>
    /// 通用参数校验（产品Key+设备名称+属性标识）
    /// 设计考量：集中校验核心参数，避免每个方法重复编写校验逻辑
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

    /// <summary>
    /// 通用参数校验（产品Key+设备名称）
    /// </summary>
    private void ValidateParameters(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentException("产品标识不能为空或空白", nameof(productKey));
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("设备名称不能为空或空白", nameof(deviceName));
        }
    }
    #endregion
}