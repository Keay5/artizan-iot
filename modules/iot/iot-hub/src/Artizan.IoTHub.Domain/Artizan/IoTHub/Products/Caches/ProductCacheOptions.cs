using System;

namespace Artizan.IoTHub.Products.Caches;

public class ProductCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public ProductCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
