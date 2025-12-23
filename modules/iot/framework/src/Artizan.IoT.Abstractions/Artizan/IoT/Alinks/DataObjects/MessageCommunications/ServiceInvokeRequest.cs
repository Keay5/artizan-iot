using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.Products.ProductMoudles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备服务调用请求（异步）（原有注释：云端向设备下发服务调用指令）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services
/// 【Topic模板】
///     - 默认模块：/sys/${productKey}/${deviceName}/thing/service/${serviceIdentifier}
///     -自定义模块：/sys/${productKey}/${deviceName}/thing/service/${moduleIdentifier}:${serviceIdentifier}
/// 【Method】：
///     - 默认模块：thing.service.${serviceIdentifier} 
///     - 自定义模块：thing.service.${moduleIdentifier}:${serviceIdentifier}
/// 【模块规则】：
///     - 默认模块：无需填写ModuleIdentifier（关联ProductModule.Identifier="Default"、TSL.functionBlockId="Default"，不区分大小写）；
///     - 自定义模块：ModuleIdentifier需与ProductModule.Identifier/TSL.functionBlockId严格一致（区分大小写）；
///     - 禁止显式传入"Default"作为ModuleIdentifier。
/// </summary>
public class ServiceInvokeRequest : AlinkRequestBase
{
    /// <summary>
    /// 模块标识（原ModuleId，对应TSL的functionBlockId/ProductModule的Identifier）
    /// - 默认模块：无需填写（系统自动关联Identifier="Default"的ProductModule，不区分大小写）；
    /// - 自定义模块：需与ProductModule的Identifier严格一致（区分大小写）。
    /// </summary>
    [JsonIgnore]
    public string? ModuleIdentifier { get; set; }

    /// <summary>
    /// 服务标识（原ServiceId，对应TSL中定义的服务ID）
    /// </summary>
    [JsonIgnore]
    public string ServiceIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 动态生成method（原有注释：根据模块和服务标识拼接协议方法名）
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModuleIdentifier))
            {
                // 默认模块：关联Identifier=Default的ProductModule
                return $"thing.service.{ServiceIdentifier}";
            }
            // 自定义模块：拼接ModuleIdentifier（区分大小写）
            return $"thing.service.{ModuleIdentifier}:{ServiceIdentifier}";
        }
    }

    /// <summary>
    /// 服务调用参数（原有注释：key为服务参数ID，value为参数值）
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// 生成Topic（原有注释：根据产品Key、设备Name、模块和服务标识生成调用Topic）
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        if (string.IsNullOrWhiteSpace(ServiceIdentifier))
        {
            throw new ArgumentNullException(nameof(ServiceIdentifier), "ServiceIdentifier不能为空");
        }

        string serviceIdentifier;
        if (string.IsNullOrWhiteSpace(ModuleIdentifier))
        {
            // 默认模块：直接使用服务标识
            serviceIdentifier = ServiceIdentifier;
        }
        else
        {
            // 自定义模块：拼接ModuleIdentifier（区分大小写）
            serviceIdentifier = $"{ModuleIdentifier}:{ServiceIdentifier}";
        }
        return $"/sys/{productKey}/{deviceName}/thing/service/{serviceIdentifier}";
    }

    /// <summary>
    /// 校验参数（原有注释：校验服务标识非空、参数合法）
    /// 【补充规则】：禁止显式传入Default作为ModuleIdentifier（默认模块无需填写）
    /// </summary>
    public ValidateResult Validate()
    {
        // 校验ModuleIdentifier规则
        if (!string.IsNullOrWhiteSpace(ModuleIdentifier) && ProductModuleHelper.IsDefaultModule(ModuleIdentifier))
        {
            return ValidateResult.Failed("默认模块无需填写ModuleIdentifier，请勿显式传入\"Default\"");
        }

        if (string.IsNullOrWhiteSpace(ServiceIdentifier))
        {
            return ValidateResult.Failed("服务标识（ServiceIdentifier）不能为空");
        }
        if (Params == null || !Params.Any())
        {
            return ValidateResult.Failed("服务调用参数不能为空");
        }
        return ValidateResult.Success();
    }
}

