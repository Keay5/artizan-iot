using System;

namespace Artizan.IoT.Products.Caches;

public class ProductTslCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public ProductTslCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
