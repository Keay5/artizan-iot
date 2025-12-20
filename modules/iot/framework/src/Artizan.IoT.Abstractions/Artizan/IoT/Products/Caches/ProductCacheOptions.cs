using System;

namespace Artizan.IoT.Products.Caches;

public class ProductCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public ProductCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
