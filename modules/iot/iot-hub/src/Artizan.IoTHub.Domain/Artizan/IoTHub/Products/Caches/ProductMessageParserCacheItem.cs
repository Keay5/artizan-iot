using Artizan.IoTHub.Products.MessageParsings;

namespace Artizan.IoTHub.Products.Caches;

public class ProductMessageParserCacheItem
{
    public ProuctMessageParserScriptLanguage MessageParserScriptLanguage { get; set; }
    public string? MessageParserScript { get; set; }

    public ProductMessageParserCacheItem()
    {
    }

    public static string CalculateCacheKey(string productKey)
    {
        return $"pk:{productKey}:Product:MessageParser";
    }
}
