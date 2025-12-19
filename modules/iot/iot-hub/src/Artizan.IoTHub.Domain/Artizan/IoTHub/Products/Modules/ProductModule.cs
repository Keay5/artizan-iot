using Artizan.IoT.Products.ProductMoudles;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Products.Modules;

/// <summary>
/// 用例：
/// https://help.aliyun.com/zh/iot/user-guide/add-a-tsl-feature?scm=20140722.S_help%40%40%E6%96%87%E6%A1%A3%40%4088241._.ID_help%40%40%E6%96%87%E6%A1%A3%40%4088241-RL_%E4%BA%A7%E5%93%81%E6%A8%A1%E5%9D%97%E6%A0%87%E8%AF%86-LOC_doc~UND~ab-OR_ser-PAR1_212a5d3d17636555027928711ddb6d-V_4-PAR3_o-RE_new5-P0_1-P1_0&spm=a2c4g.11186623.help-search.i19
/// 
/// API：
/// https://help.aliyun.com/zh/iot/developer-reference/api-a99t11?spm=a2c4g.11186623.help-menu-30520.d_4_0_6_9_0.27cd61daU05P3d
/// </summary>
public class ProductModule : FullAuditedAggregateRoot<Guid>
{
    [NotNull]
    public virtual Guid ProductId { get; protected set; }

    [NotNull]
    public virtual string Name { get; protected set; }

    [NotNull]
    public virtual string Identifier { get; protected set; }

    [NotNull]
    public virtual bool IsDefault { get; protected set; }

    [NotNull]
    public ProductModuleStatus Status { get; set; }

    /// <summary>
    ///  DateTimeOffset timestamp = DateTimeOffset.Now;
    ///  string version = timestamp.ToUnixTimeSeconds().ToString(); // 将时间戳转换为 Unix 时间戳格式的字符串
    /// </summary>
    [CanBeNull]
    public string? Version { get; set; }

    /// <summary>
    /// 是否是当前最新版本， true表示是最新版本， false表示不是最新版本
    /// </summary>
    [NotNull]
    public bool IsCurrentVersion { get; protected set; }

    [CanBeNull]
    public virtual string? Description { get; protected set; }

    [NotNull]
    public virtual string ProductModuleTsl { get; protected set; }

    protected ProductModule() 
    {
    }

    public ProductModule(
        Guid id, 
        Guid productId, 
        string name, 
        string identifier, 
        bool isDefault, 
        ProductModuleStatus status, 
        string? version,
        bool isCurrentVersion,
        string tsl,
        string? description) : base(id)
    {
        Id = id;
        ProductId = productId;
        SetName(name);
        SetIdentifier(identifier);
        IsDefault = isDefault;
        Status = status;
        Version = version;
        IsCurrentVersion = isCurrentVersion;
        SetDescription(description);
        SetProductModuleTsl(tsl);
    }

    /// <summary>
    /// 模块的名称，例如：用电量。同一产品下功能名称不能重复。
    /// 支持中文、英文字母、数字和下划线，长度限制 4～30 个字符。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual ProductModule SetName([NotNull] string name)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        if (name.Length > ProductModuleConsts.MaxNameLength ||
            !Regex.IsMatch(name, ProductModuleConsts.NameCharRegexPattern)
            )
        {
            throw new BusinessException(IoTHubErrorCodes.ProductModuleNameInvalid)
                .WithData("MaxNameLength", ProductModuleConsts.MaxNameLength);
        }

        Name = name;

        return this;
    }

    /// <summary>
    /// 产品模块标识符，在产品中具有唯一性。
    /// - 模块标识符，在产品中具有唯一性。支持英文大小写字母、数字和下划线（_），不超过30个字符。
    ///   https://help.aliyun.com/zh/iot/developer-reference/api-a99t11?spm=a2c4g.11186623.help-menu-30520.d_4_0_6_9_0.40905780IsvLA0&scm=20140722.H_150323._.OR_help-T_cn~zh-V_1
    /// - 限制：
    ///   不能用以下系统保留参数作为标识符：set、get、post、property、event、time、value。
    ///   https://help.aliyun.com/zh/iot/user-guide/add-a-tsl-feature?scm=20140722.S_help%40%40%E6%96%87%E6%A1%A3%40%4088241._.ID_help%40%40%E6%96%87%E6%A1%A3%40%4088241-RL_%E4%BA%A7%E5%93%81%E6%A8%A1%E5%9D%97%E6%A0%87%E8%AF%86-LOC_doc%7EUND%7Eab-OR_ser-PAR1_212a5d3d17636555027928711ddb6d-V_4-PAR3_o-RE_new5-P0_1-P1_0&spm=a2c4g.11186623.help-search.i19
    ///   
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public virtual ProductModule SetIdentifier([NotNull] string identifier)
    {
        Check.NotNullOrWhiteSpace(identifier, nameof(identifier));

        if (identifier.Length > ProductModuleConsts.MaxIdentifierLength ||                  
            !Regex.IsMatch(identifier, ProductModuleConsts.IdentifierCharRegexPattern) ||   
            ProductModuleConsts.SystemReservedIdentifiers.Contains(identifier.ToLower())    //检查是否为保留关键字（不区分大小写）
           )
        {
            throw new BusinessException(IoTHubErrorCodes.ProductModuleIdentifierInvalid)
                .WithData("MaxLength", ProductModuleConsts.MaxIdentifierLength);
        }
        
        Identifier = identifier;

        return this;
    }

    public virtual ProductModule SetVersion([CanBeNull] string? version)
    {
        if (!version.IsNullOrEmpty() && version.Length > ProductModuleConsts.MaxVersionLength)
        {
            throw new ArgumentException($"Version can not be longer than {ProductModuleConsts.MaxVersionLength}");
        }

        Version = version;

        return this;
    }
    public virtual ProductModule SetDescription(string? description)
    {
        if (description.IsNullOrEmpty())
        {
            return this;
        }

        if (!description.IsNullOrWhiteSpace() &&
             description.Length >= ProductModuleConsts.MaxDescriptionLength
           )
        {
            throw new BusinessException(IoTHubErrorCodes.DescriptionInvalid)
                 .WithData("MaxDescriptionLength", ProductModuleConsts.MaxDescriptionLength);
        }

        Description = description;

        return this;
    }

    public virtual ProductModule SetProductModuleTsl([NotNull] string tsl)
    {
        Check.NotNullOrWhiteSpace(tsl, nameof(tsl));

        ProductModuleTsl = tsl;

        return this;
    }
}
