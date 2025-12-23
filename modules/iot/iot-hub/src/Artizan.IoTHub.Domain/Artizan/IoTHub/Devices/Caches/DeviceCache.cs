using Artizan.IoTHub.Products;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Security.Encryption;

namespace Artizan.IoTHub.Devices.Caches;

/// <summary>
/// 设备缓存。
/// </summary>
public class DeviceCache : ITransientDependency
{
    protected ILogger<DeviceCache> Logger { get; set; }
    /// <summary>
    /// String Encryption:
    /// https://abp.io/docs/latest/framework/infrastructure/string-encryption?_redirected=B8ABF606AA1BDF5C629883DF1061649A
    /// </summary>
    protected IStringEncryptionService StringEncryptionService { get; }
    protected IDistributedCache<DeviceCacheItem, string> Cache { get; }
    protected IOptions<DeviceCacheOptions> CacheOptions { get; }
    protected IObjectMapper ObjectMapper { get; }
    protected IDeviceRepository DeviceRepository { get; }
    protected IProductRepository ProductRepository { get; }

    public DeviceCache(
        ILogger<DeviceCache> logger,
        IStringEncryptionService stringEncryptionService,
        IDistributedCache<DeviceCacheItem, string> cache,
        IOptions<DeviceCacheOptions> cacheOptions,
        IObjectMapper objectMapper,
        IDeviceRepository deviceRepository,
        IProductRepository productRepository)
    {
        Logger = logger;//NullLogger<DeviceCache>.Instance;
        StringEncryptionService = stringEncryptionService;
        Cache = cache;
        CacheOptions = cacheOptions;
        ObjectMapper = objectMapper;
        DeviceRepository = deviceRepository;
        ProductRepository = productRepository;
    }

    public virtual async Task<DeviceCacheItem> GetAsync(string productKey, string deviceName)
    {
        Logger.LogDebug($"Get cache for device | productKey: {productKey}/deviceName:{deviceName}");

        var cacheItem = await Cache.GetOrAddAsync(
            DeviceCacheItem.CalculateCacheKey(productKey, deviceName),
            () => InternalGetAsync(productKey, deviceName),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            });

        return cacheItem!;
    }

    public virtual async Task<string> GetDeviceSecretAsync(string productKey, string deviceName)
    {
        return StringEncryptionService.Decrypt((await GetAsync(productKey, deviceName)).EncryptedDeviceSecret) ?? string.Empty;
    }

    public virtual Task SetAsync(string productKey, string deviceName, DeviceCacheItem cacheItem, bool considerUow = true)
    {
        Logger.LogDebug($"Set device cache. | productKey: {productKey}/deviceName:{deviceName}");
        return Cache.SetAsync(
            DeviceCacheItem.CalculateCacheKey(productKey, deviceName),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheOptions.Value.CacheAbsoluteExpiration
            },
            considerUow: considerUow
        );
    }

    public virtual async Task ResetAsync(string productKey, string deviceName, DeviceCacheItem cacheItem, bool considerUow = true)
    {
        Logger.LogDebug($"Reset device cache. | productKey: {productKey}/deviceName:{deviceName}");
        await ClearAsync(productKey, deviceName, considerUow);
        await SetAsync(productKey, deviceName, cacheItem, considerUow:true);
    }

    public virtual async Task ClearAsync(string productKey, string deviceName, bool considerUow = true)
    {
        Logger.LogDebug($"Remove device cache. | productKey: {productKey}/deviceName:{deviceName}");
        await Cache.RemoveAsync(DeviceCacheItem.CalculateCacheKey(productKey, deviceName), considerUow: considerUow);
    }

    public virtual DeviceCacheItem Map(Device device, string productKey)
    {
        // 基础字段映射
        var cacheItem = ObjectMapper.Map<Device, DeviceCacheItem>(device);
        // 处理特殊字段
        cacheItem.EncryptedDeviceSecret = StringEncryptionService.Encrypt(device.DeviceSecret) ?? string.Empty;
        cacheItem.ProductKey = productKey;

        return cacheItem;
    }

    private async Task<DeviceCacheItem> InternalGetAsync(string productKey, string deviceName)
    {
        var productId = await ProductRepository.GetIdByProductKeyAsync(productKey);
        var device = await DeviceRepository.GetByDeviceNameAsync(productId, deviceName);
        var cacheItem = Map(device, productKey);

        return cacheItem;
    }
}