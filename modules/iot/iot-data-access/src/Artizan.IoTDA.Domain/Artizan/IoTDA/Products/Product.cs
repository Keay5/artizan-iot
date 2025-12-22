using Artizan.IoTDA.Localization;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Artizan.IoTDA.Products;

public class Product : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 产品ID，用于唯一标识一个产品。
    /// 如果携带此参数，平台将产品ID设置为该参数值；
    /// 如果不携带此参数，产品ID在物联网平台创建产品后由平台分配获得
    /// </summary>
    public string ProductKey { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public ProtocolType ProtocolType { get; set; }

    /// <summary>
    /// 数据格式
    /// </summary>
    public DataFormat DataFormat { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// 设备类型名称
    /// </summary>
    public string DeviceTypeName{ get; private set; }

    /// <summary>
    /// 产品描述
    /// </summary>
    public string? Description { get; private set; }

    protected Product()
    {
    }

    public Product(
        string productKey,
        string productName,
        ProtocolType protocolType,
        DataFormat dataFormat,
        DeviceType deviceType,
        string deviceTypeName,
        string? description = null
        )
    {
        SetProductKey(productKey);
        SetProductName(productName);
        ProtocolType = protocolType;
        DataFormat = dataFormat;
        DeviceType = deviceType;
        SetDeviceTypeName(deviceTypeName);
        SetDescription(description);
    }

    /// <summary>
    /// 设置产品Key。
    /// </summary>
    /// <param name="productKey"></param>
    /// <returns></returns>
    public virtual Product SetProductKey(string productKey)
    {
        Check.NotNullOrWhiteSpace(productKey, nameof(productKey), IoTDAConsts.Product.MaxProductKeyLength);

        // 名称字符合法性校验
        var isMatch = Regex.IsMatch(productKey, IoTDAConsts.Product.ProductKeyCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTDAErrorCodes.ProductKeyInvalid)
                 .WithData("MaxProductKeyLength", IoTDAConsts.Product.MaxProductKeyLength);

        }

        ProductName = productKey;

        return this;
    }

    /// <summary>
    /// 设置产品名称。
    /// 产品名称长度不超过64个字符，只允许中文、字母、数字、以及_?'#().,&%@!-等字符的组合。 
    /// </summary>
    /// <param name="productName"></param>
    /// <returns></returns>
    public virtual Product SetProductName(string productName)
    {
        Check.NotNullOrWhiteSpace(productName, nameof(productName), IoTDAConsts.Product.MaxProductNameLength);

        // 名称字符合法性校验
        var isMatch = Regex.IsMatch(productName, IoTDAConsts.Product.ProductNameCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTDAErrorCodes.ProductNameInvalid)
                .WithData("MaxProductNameLength", IoTDAConsts.Product.MaxProductNameLength);
        }

        ProductName = productName;

        return this;
    }

    /// <summary>
    /// 设置设备类型名称。
    /// 设备类型名称长度不超过32个字符，只允许中文、字母、数字、以及_?'#().,&%@!-等字符的组合。 
    /// </summary>
    /// <param name="deviceTypeName"></param>
    /// <returns></returns>
    public virtual Product SetDeviceTypeName(string deviceTypeName)
    {
        Check.NotNullOrWhiteSpace(deviceTypeName, nameof(deviceTypeName), IoTDAConsts.Product.MaxDeviceTypeNameLength);

        // 名称字符合法性校验
        var isMatch = Regex.IsMatch(deviceTypeName, IoTDAConsts.Product.DeviceTypeNameCharRegexPattern);
        if (!isMatch)
        {
            throw new BusinessException(IoTDAErrorCodes.DeviceTypeNameInvalid)
                .WithData("MaxDeviceTypeNameLength", IoTDAConsts.Product.MaxDeviceTypeNameLength);
        }

        ProductName = deviceTypeName;

        return this;
    }

    public virtual Product SetDescription(string description)
    {

        if (!description.IsNullOrWhiteSpace() && 
             description.Length >= IoTDAConsts.Product.MaxDescriptionLength
           )
        {
            throw new BusinessException(IoTDAErrorCodes.DescriptionInvalid)
            .WithData("MaxDescriptionLength", IoTDAConsts.Product.MaxDescriptionLength);
        }

        Description = description;

        return this;
    }

}
