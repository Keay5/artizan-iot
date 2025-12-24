using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Artizan.IoTDA.ProductModels;

public class ProductModelService : FullAuditedAggregateRoot<Guid>
{
    public string ServiceId { get; private set; }
    public string ServiceType { get; set; }
    public string Description { get; set; }

    protected ProductModelService()
    {
    }

    public ProductModelService(string serviceId, string serviceType,  string description)
    {
        ServiceId = serviceId;
        ServiceType = serviceType;
        Description = description;
    }


    /// <summary>
    /// 设置产品模型的服务ID
    /// 产品名称长度不超过64个字符，只允许中文、字母、数字、以及_?'#().,&%@!-等字符的组合。 
    /// </summary>
    /// <param name="serviceId"></param>
    /// <returns></returns>
    public virtual ProductModelService SetProductModelServiceId(string serviceId)
    {
        Check.NotNullOrWhiteSpace(serviceId, nameof(serviceId), IoTDAConsts.ProductModel.MaxProductModelServiceIdLength);

        // 名称字符合法性校验
        //var isMatch = Regex.IsMatch(serviceId, IoTDAConsts.ProductModel.ProductModelServiceIdCharRegexPattern);
        //if (!isMatch)
        //{
        //    throw new BusinessException(IoTDAErrorCodes.ProductNameInvalid)
        //        .WithData("MaxProductNameLength", IoTDAConsts.ProductModel.MaxDescriptionLength);
        //}

        ServiceId = serviceId;

        return this;
    }
}
