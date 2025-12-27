using Artizan.IoT.Things.Caches.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Caching;

namespace Artizan.IoT.Things.Caches;

/// <summary>
/// 缓存管理抽象基类（统一处理Key前缀）
/// </summary>
public abstract class ThingPropertyDataCacheManagerBase : IThingPropertyDataCacheManager
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
    public ThingPropertyDataCacheManagerBase(IOptions<AbpDistributedCacheOptions> abpDistributedCacheOptions)
    {
        _abpDistributedCacheOptions = abpDistributedCacheOptions.Value;

        // ========== 核心：从当前类的程序集自动获取项目名称 ==========
        // 1. 获取当前类所属的程序集（GetType().Assembly 比 Assembly.GetExecutingAssembly() 更可靠，适配继承场景）
        //Assembly currentAssembly = GetType().Assembly;
        // 2. 提取程序集名称（核心：替代硬编码）
       // string assemblyName = currentAssembly.GetName().Name;
        //// 3. 清理冗余后缀（可选：根据项目命名规范调整）
        //string cleanProjectName = CleanAssemblyName(assemblyName);
        //// 4. 拼接为缓存前缀（末尾加冒号，保证Key格式统一）
        //_projectPrefix = $"{cleanProjectName}:";
        // _projectPrefix = $"{assemblyName}:";
    }

    public abstract Task<bool> ExistsAsync(string productKey, string deviceName, string propertyIdentifier, CancellationToken cancellationToken = default);
    public abstract Task<ThingPropertyDataCacheItem?> GetAsync(string productKey, string deviceName, string propertyIdentifier, CancellationToken cancellationToken = default);
    public abstract Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(string productKey, string deviceName, IEnumerable<string> propertyIdentifiers, CancellationToken cancellationToken = default);
    public abstract Task RemoveAsync(string productKey, string deviceName, string propertyIdentifier, CancellationToken cancellationToken = default);
    public abstract Task RemoveDeviceAllPropertiesAsync(string productKey, string deviceName, CancellationToken cancellationToken = default);
    public abstract Task RemoveManyAsync(string productKey, string deviceName, IEnumerable<string> propertyIdentifiers, CancellationToken cancellationToken = default);
    public abstract Task SetAsync(ThingPropertyDataCacheItem cacheItem, CacheExpireMode expireMode = CacheExpireMode.Absolute, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    public abstract Task SetManyAsync(IEnumerable<ThingPropertyDataCacheItem> cacheItems, CacheExpireMode expireMode = CacheExpireMode.Absolute, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理程序集名称的冗余后缀（适配不同项目命名规范）
    /// 示例：Artizan.IoT.Core → Artizan.IoT；Artizan.IoT.HttpApi → Artizan.IoT
    /// </summary>
    /// <param name="assemblyName">原始程序集名称</param>
    /// <returns>清理后的简洁项目名称</returns>
    protected virtual string CleanAssemblyName(string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        { 
            return "Artizan.IoT"; 
            // 兜底默认值
        }

        // 自定义清理规则（根据你的项目命名规范调整）
        var redundantSuffixes = new[] { ".Core", ".HttpApi", ".Host", ".Application", ".Domain", ".Infrastructure" };
        foreach (var suffix in redundantSuffixes)
        {
            if (assemblyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = assemblyName[..^suffix.Length];
                break; // 匹配到第一个后缀即停止（可根据需要改为循环清理所有）
            }
        }
        return assemblyName;
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

        // 最终Key格式：AbpDistributedCacheOptions.KeyPrefix + 项目前缀 + 原始Key
        // 示例：MsOnAbp:Artizan.IoT:thing:cache:pk:a1B2c3:dn:Device001
        //var appPrefix = _cacheOptions.KeyPrefix ?? string.Empty;
        //return $"{appPrefix}:{_projectPrefix}:{originalKey}";

        //注意：不加入_projectPrefix，避免微服务架构下，微服务间无法共享缓存
        var appPrefix = !string.IsNullOrWhiteSpace(_abpDistributedCacheOptions.KeyPrefix)
            ? $"{_abpDistributedCacheOptions.KeyPrefix}:"
            : "";
        return $"{appPrefix}{originalKey}";
    }
}
