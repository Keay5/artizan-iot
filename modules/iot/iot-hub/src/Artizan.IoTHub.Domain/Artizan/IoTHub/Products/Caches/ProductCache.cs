using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Security.Encryption;

namespace Artizan.IoTHub.Products.Caches;

/// <summary>
/// 产品缓存。
/// </summary>
public class ProductCache : ITransientDependency
{
    public ILogger<ProductCache> Logger { get; set; }

    /// <summary>
    /// String Encryption:
    /// https://abp.io/docs/latest/framework/infrastructure/string-encryption?_redirected=B8ABF606AA1BDF5C629883DF1061649A
    /// </summary>
    protected IStringEncryptionService StringEncryptionService { get; }

    protected IDistributedCache<ProductCacheItem, string> Cache { get; }
    protected IOptions<ProductCacheOptions> CacheOptions { get; }
    protected IObjectMapper ObjectMapper { get; }

    protected IProductRepository ProductRepository { get; }

    public ProductCache(
        ILogger<ProductCache> logger,
        IStringEncryptionService stringEncryptionService,
        IDistributedCache<ProductCacheItem, string> cache,
        IOptions<ProductCacheOptions> cacheOptions,
        IObjectMapper objectMapper,
        IProductRepository productRepository)
    {
        Logger = logger;//NullLogger<ProductCache>.Instance;
        StringEncryptionService = stringEncryptionService;
        Cache = cache;
        CacheOptions = cacheOptions;
        ObjectMapper = objectMapper;
        ProductRepository = productRepository;
    }

    public virtual async Task<ProductCacheItem> GetAsync(string productKey)
    {
        Logger.LogDebug($"Get cache for product. | productKey: {productKey}");

        var cacheItem = await Cache.GetOrAddAsync(
            ProductCacheItem.CalculateCacheKey(productKey),
            () => InternalGetAsync(productKey),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            });

        return cacheItem!;
    }

    public virtual async Task<string> GetProductSecretAsync(string productKey)
    {
        return StringEncryptionService.Decrypt((await GetAsync(productKey)).EncryptedProductSecret) ?? string.Empty;
    }

    public virtual Task SetAsync(string productKey, ProductCacheItem cacheItem, bool considerUow = true)
    {
        Logger.LogDebug($"Set product cache. | productKey: {productKey}");
        return Cache.SetAsync(
            ProductCacheItem.CalculateCacheKey(productKey),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            },
            considerUow: considerUow
        );
    }

    public virtual async Task ResetAsync(string productKey, ProductCacheItem cacheItem, bool considerUow = true)
    {
        Logger.LogDebug($"Reset product cache. | productKey: {productKey}");
        await ClearAsync(ProductCacheItem.CalculateCacheKey(productKey), considerUow: considerUow);
        await SetAsync(productKey, cacheItem, considerUow);
    }

    public virtual async Task ClearAsync(string productKey, bool considerUow = true)
    {
        Logger.LogDebug($"Remove product cache. | productKey: {productKey}");
        await Cache.RemoveAsync(ProductCacheItem.CalculateCacheKey(productKey), considerUow: considerUow);
    }

    public virtual ProductCacheItem Map(Product product)
    {
        // 基础字段映射 
        var cacheItem = ObjectMapper.Map<Product, ProductCacheItem>(product);
        // 处理特殊字段
        cacheItem.EncryptedProductSecret = StringEncryptionService.Encrypt(product.ProductSecret) ?? string.Empty;

        return cacheItem;
    }

    private async Task<ProductCacheItem> InternalGetAsync(string productKey)
    {
        var product = await ProductRepository.GetByProductKeyAsync(productKey);
        var cacheItem = Map(product);

        return cacheItem;
    }
}
