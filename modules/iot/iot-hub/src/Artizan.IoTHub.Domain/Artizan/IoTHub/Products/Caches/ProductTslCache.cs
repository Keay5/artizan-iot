using Artizan.IoT.Products.Caches;
using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Artizan.IoTHub.Products.Modules;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Products.Caches;

/// <summary>
/// 产品物模型TSL缓存。
/// </summary>
public class ProductTslCache : ITransientDependency
{
    public ILogger<ProductTslCache> Logger { get; set; }
    protected IDistributedCache<ProductTslCacheItem, string> Cache { get; }
    protected IOptions<ProductTslCacheOptions> CacheOptions { get; }

    protected IProductRepository ProductRepository { get; }
    protected IProductModuleRepository ProductModuleRepository { get; }

    public ProductTslCache(
        ILogger<ProductTslCache> logger,
        IDistributedCache<ProductTslCacheItem, string> cache,
        IOptions<ProductTslCacheOptions> cacheOptions,
        IProductRepository productRepository,
        IProductModuleRepository productModuleRepository)
    {
        Logger = logger;
        Cache = cache;
        CacheOptions = cacheOptions;

        ProductRepository = productRepository;
        ProductModuleRepository = productModuleRepository;
    }

    public virtual async Task<ProductTslCacheItem> GetAsync(string productKey)
    {
        Logger.LogDebug($"Get cache for product tls.| productKey: {productKey}");

        var cacheItem = await Cache.GetOrAddAsync(
            ProductTslCacheItem.CalculateCacheKey(productKey),
            () => InternalGetAsync(productKey),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            });

        return cacheItem!;
    }

    public virtual Task SetAsync(string productKey, ProductTslCacheItem cacheItem, bool considerUow = true)
    {
        Logger.LogDebug($"Set product tsl cache. | productKey: {productKey}");
        return Cache.SetAsync(
            ProductTslCacheItem.CalculateCacheKey(productKey),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            },
            considerUow
        );
    }

    public virtual Task SetAsync(string productKey, List<ProductModule> productModules, bool considerUow = true)
    {
        Logger.LogDebug($"Set product tsl cache. | productKey: {productKey}");
        return SetAsync(productKey, Map(productModules), considerUow);
    }

    public virtual async Task ResetAsync(string productKey, List<ProductModule> productModules, bool considerUow = true)
    {
        Logger.LogDebug($"ReSet product tsl cache. | productKey: {productKey}");

        /*
         敏感场景（如缓存结构变更、高并发更新、分布式一致性要求高）：先 RemoveAsync 再 SetAsync，或结合分布式锁进一步提升安全性。
         */
        await ClearAsync(ProductTslCacheItem.CalculateCacheKey(productKey), considerUow);
        await SetAsync(productKey, productModules, considerUow);
    }

    public virtual async Task ClearAsync(string productKey, bool considerUow = true)
    {
        Logger.LogDebug($"Remove product tsl cache. | productKey: {productKey}");
        await Cache.RemoveAsync(ProductTslCacheItem.CalculateCacheKey(productKey), considerUow);
    }

    public virtual ProductTslCacheItem Map(List<ProductModule> productModules)
    {
        var cacheItem = new ProductTslCacheItem();

        foreach (var module in productModules)
        {
            var tsl = TslSerializer.DeserializeObject<Tsl>(module.ProductModuleTsl);
            if (tsl != null)
            {
                cacheItem.Tsls.Add(tsl);
            }
        }
        return cacheItem;
    }

    private async Task<ProductTslCacheItem> InternalGetAsync(string productKey)
    {
        var productId = await ProductRepository.GetIdByProductKeyAsync(productKey);
        var productModules = await ProductModuleRepository.GetCurrentVersionListByProductIdAsync(productId);

        return Map(productModules);
    }
}
