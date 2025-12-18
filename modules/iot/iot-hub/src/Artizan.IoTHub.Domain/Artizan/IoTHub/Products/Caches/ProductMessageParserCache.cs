using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Artizan.IoTHub.Products.MessageParsings;
using Artizan.IoTHub.Products.Modules;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Products.Caches;

/// <summary>
/// 产品消息解析器缓存。
/// </summary>
public class ProductMessageParserCache : ITransientDependency
{
    public ILogger<ProductMessageParserCache> Logger { get; set; }
    protected IDistributedCache<ProductMessageParserCacheItem, string> Cache { get; }
    protected IOptions<ProductMessageParserCacheOptions> CacheOptions { get; }

    protected IProductRepository ProductRepository { get; }
    protected IProductModuleRepository ProductModuleRepository { get; }
    protected IProductMessageParserRepository ProductMessageParserRepository { get; }


    public ProductMessageParserCache(
        ILogger<ProductMessageParserCache> logger,
        IDistributedCache<ProductMessageParserCacheItem, string> cache,
        IOptions<ProductMessageParserCacheOptions> cacheOptions,
        IProductRepository productRepository,
        IProductModuleRepository productModuleRepository,
        IProductMessageParserRepository productMessageParserRepository)
    {
        Logger = logger;
        Cache = cache;
        CacheOptions = cacheOptions;

        ProductRepository = productRepository;
        ProductModuleRepository = productModuleRepository;
        ProductMessageParserRepository = productMessageParserRepository;
    }

    public virtual async Task<ProductMessageParserCacheItem> GetAsync(string productKey)
    {
        Logger.LogDebug($"Get cache for product mesaage parser. | productKey: {productKey}");

        var cacheItem = await Cache.GetOrAddAsync(
            ProductMessageParserCacheItem.CalculateCacheKey(productKey),
            () => InternalGetAsync(productKey),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            });

        return cacheItem!;
    }

    private async Task<ProductMessageParserCacheItem> InternalGetAsync(string productKey)
    {
        var cacheItem = new ProductMessageParserCacheItem();

        var productId = await ProductRepository.GetIdByProductKeyAsync(productKey);
        var messageParser = await ProductMessageParserRepository.FindPublishedByProductIdAsync(productId);

        if (messageParser != null)
        {
            cacheItem.MessageParserScriptLanguage = messageParser.ScriptLanguage;
            cacheItem.MessageParserScript = messageParser.Script;
        }


        return cacheItem;
    }

    public virtual async Task ClearAsync(string productKey, bool considerUow = true)
    {
        Logger.LogDebug($"Remove product messager parser cache. | productKey: {productKey}");
        await Cache.RemoveAsync(ProductMessageParserCacheItem.CalculateCacheKey(productKey), considerUow);
    }
}
