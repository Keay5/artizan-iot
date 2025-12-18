using System;

namespace Artizan.IoTHub.Products.Caches;

public class ProductMessageParserCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public ProductMessageParserCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
