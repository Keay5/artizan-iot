using Artizan.IoT.Products.Properties;
using System;

namespace Artizan.IoT.Products.Caches;

[Serializable]
public class ProductCacheItem
{
    public Guid Id { get; set; }
    public string ProductKey { get; set; }
    public string ProductName { get; set; }
    public ProductNodeTypes NodeType { get; set; }
    public ProductNetworkingModes? NetworkingMode { get; set; }
    public ProductAccessGatewayProtocol? AccessGatewayProtocol { get; set; }
    public ProductDataFormat DataFormat { get; set; }
    public ProductAuthenticationMode AuthenticationMode { get; set; }
    public bool IsUsingPrivateCACertificate { get; set; }
    public bool IsEnableDynamicRegistration { get; set; }
    public ProductStatus ProductStatus { get; set; }

    /// <summary>
    /// 存储加密后的密文（缓存中实际保存的值）
    /// 加密： StringEncryptionService.Encrypt(ProductSecret)
    /// 解密时使用：StringEncryptionService.Decrypt(EncryptedProductSecret)   
    /// </summary>
    public string EncryptedProductSecret { get; set; }

    public ProductCacheItem()
    {
    }

    public static string CalculateCacheKey(string productKey)
    {
        return $"pk:{productKey}:Product";
    }
}
