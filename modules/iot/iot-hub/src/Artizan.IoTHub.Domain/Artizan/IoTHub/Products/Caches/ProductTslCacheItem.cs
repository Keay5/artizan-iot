using Artizan.IoT.ThingModels.Tsls;
using System.Collections.Generic;

namespace Artizan.IoTHub.Products.Caches;

public class ProductTslCacheItem
{
    public List<Tsl> Tsls { get; set; } = new();

    public ProductTslCacheItem()
    {
    }

    public static string CalculateCacheKey(string productKey)
    {
        return $"pk:{productKey}:Product:Tsl";
    }
}
