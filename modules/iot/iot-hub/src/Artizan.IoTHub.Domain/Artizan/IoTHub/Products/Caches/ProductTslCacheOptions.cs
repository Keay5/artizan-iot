using System;

namespace Artizan.IoTHub.Products.Caches;

public class ProductTslCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public ProductTslCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
