using Artizan.IoT.Products.MessageParsings;

namespace Artizan.IoT.Products.Caches;

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
