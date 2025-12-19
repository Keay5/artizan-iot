using Artizan.IoT.Products;
using Artizan.IoT.Products.Properties;
using Artizan.IoTHub.Utils;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Artizan.IoTHub.Products;

/// <summary>
/// 产品（设备模型）, 
/// 产品是设备的集合，通常是一组具有相同功能定义的设备集合。
/// 例如，产品指同一个型号的产品，设备就是该型号下的某个设备
/// https://help.aliyun.com/zh/iot/user-guide/create-a-product?spm=5176.11485173.help.dexternal.22fc59aflAwS9h
/// </summary>
public class Product : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 产品Key是物联网平台为每个产品分配的唯一标识。
    /// 建议： ProductKey 建议使用 GUID ，这样在分布式架构中，也能做到全局唯一。
    /// 这样在节点（比如：IoTEdge）的设备汇到一个公共平台（如：云端IoT平台）后，就不用处理太多的逻辑。
    /// </summary>
    [NotNull]
    public virtual string ProductKey { get; protected set; }
    /// <summary>
    /// ProductSecret 是由物联网平台颁发的产品密钥。
    /// 通常与 ProductKey 成对出现，可用于一型一密的认证方案。该参数很重要，需要您保管好，不能泄露
    /// </summary>
    public virtual string ProductSecret { get; protected set; }

    [NotNull]
    public virtual string ProductName { get; protected set; }

    [NotNull]
    public virtual ProductCategory Category { get; set; }

    [NotNull]
    public virtual string CategoryName { get; protected set; }

    [NotNull]
    public virtual ProductNodeTypes NodeType { get; set; }

    [CanBeNull]
    public virtual ProductNetworkingModes? NetworkingMode { get; set; }

    [CanBeNull]
    public virtual ProductAccessGatewayProtocol? AccessGatewayProtocol { get; set; }

    [NotNull]
    public virtual ProductDataFormat DataFormat { get; set; }

    [NotNull]
    public virtual ProductAuthenticationMode AuthenticationMode { get; set; }

    [NotNull]
    public virtual bool IsUsingPrivateCACertificate { get; set; }

    /// <summary>
    /// 设备动态注册无需一一烧录设备证书，
    /// 每台设备烧录相同的产品证书，即 ProductKey 和 ProductSecret ，
    /// 云端鉴权通过后下发设备证书，您可以根据需要开启或关闭动态注册，保障安全性。
    /// 
    /// 参考资料：
    /// https://help.aliyun.com/zh/iot/user-guide/unique-certificate-per-product-verification?spm=a2c4g.11186623.0.i6
    /// </summary>
    [NotNull]
    public virtual bool IsEnableDynamicRegistration { get; set; }

    [NotNull]
    public virtual ProductStatus ProductStatus { get; set; }

    [CanBeNull]
    public virtual string? Description { get; protected set; }

    protected Product()
    {

    }

    public Product(Guid id, [NotNull] string productKey, string productSecret, [NotNull] string productName)
    {
        Id = id;
        SetProductKey(productKey);
        SetProductSecret(productSecret);
        SetProductName(productName);
    }

    /// <summary>
    /// 设置产品Key。
    /// </summary>
    /// <param name="productKey"></param>
    /// <returns></returns>
    public virtual Product SetProductKey([NotNull] string productKey)
    {
        Check.NotNullOrWhiteSpace(productKey, nameof(productKey), ProductConsts.MaxProductKeyLength);

        var isMatch = Regex.IsMatch(productKey, ProductConsts.ProductKeyCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTHubErrorCodes.ProductKeyInvalid)
                 .WithData("MaxLength", ProductConsts.MaxProductKeyLength);
        }

        ProductKey = productKey;

        return this;
    }

    public virtual Product SetProductSecret([NotNull] string productSecret)
    {
        ProductSecret = Check.NotNullOrWhiteSpace(productSecret, nameof(productSecret), ProductConsts.MaxProductSecretLength);

        return this;
    }

    /// <summary>
    /// 设置产品名称。
    /// 产品名称长度为 4~30 个字符，可以包含中文、英文字母、数字和下划线（_）。一个中文算 2 个字符。
    /// </summary>
    /// <param name="productName">产品名称</param>
    /// <returns></returns>
    public virtual Product SetProductName([NotNull] string productName)
    {
        Check.NotNullOrWhiteSpace(productName, nameof(productName));

        int charLength = StringUtils.CalculateCharacterLength(productName);

        if (charLength < ProductConsts.MinProductNameLength || charLength > ProductConsts.MaxProductNameLength)
        {
            throw new BusinessException(IoTHubErrorCodes.ProductNameInvalid)
                .WithData("MinLength", ProductConsts.MinProductNameLength)
                .WithData("MaxLength", ProductConsts.MaxProductNameLength);
        }

        // 不要使用如下方法，结果与预期不符。比如输入“测试@123”，预期不匹配，但实际结果是匹配成功。
        //Regex ProductNameRegex = new Regex(ProductConsts.ProductNameCharRegexPattern, RegexOptions.Compiled);
        //var isMatch = !ProductNameRegex.IsMatch(productName);

        var isMatch = Regex.IsMatch(productName, ProductConsts.ProductNameCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTHubErrorCodes.ProductNameInvalid)
                .WithData("MinLength", ProductConsts.MinProductNameLength)
                .WithData("MaxLength", ProductConsts.MaxProductNameLength);
        }

        ProductName = productName;

        return this;
    }

    /// <summary>
    /// 产品品类名称。
    /// 设备类型名称长度不超过32个字符，只允许中文、字母、数字、以及_?'#().,&%@!-等字符的组合。 
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public virtual Product SetCategoryName(string categoryName)
    {
        Check.NotNullOrWhiteSpace(categoryName, nameof(categoryName), ProductConsts.MaxCategoryNameLength);

        var isMatch = Regex.IsMatch(categoryName, ProductConsts.CategoryNameCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTHubErrorCodes.ProducCategoryNameInvalid)
                .WithData("MaxCategoryNameLength", ProductConsts.MaxCategoryNameLength);
        }

        CategoryName = categoryName;

        return this;
    }

    public virtual Product SetDescription(string? description)
    {
        if (description.IsNullOrEmpty())
        {
            return this;
        }


        if (!description.IsNullOrWhiteSpace() &&
             description.Length > ProductConsts.MaxDescriptionLength
           )
        {
            throw new BusinessException(IoTHubErrorCodes.DescriptionInvalid)
            .WithData("MaxDescriptionLength", ProductConsts.MaxDescriptionLength);
        }

        Description = description;

        return this;
    }
}
