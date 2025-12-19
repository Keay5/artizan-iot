using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备服务调用请求（异步）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services
/// Topic模板（默认模块）：/sys/${productKey}/${deviceName}/thing/service/${serviceId}
/// Topic模板（自定义模块）：/sys/${productKey}/${deviceName}/thing/service/${moduleId}:${serviceId}
/// Method：thing.service.${serviceId}
///         thing.service.${moduleId}:${serviceId}
/// </summary>
public class ServiceInvokeRequest : AlinkRequestBase
{
    /// <summary>
    /// 模块ID（自定义模块必填）,对应TSL中的<see cref="ThingModels.Tsls.Tsl.FunctionBlockId"/>
    /// </summary>
    [JsonIgnore]
    public string? ModuleId { get; set; }

    /// <summary>
    /// 服务ID
    /// 对应TSL中的<see cref="ThingModels.Tsls.MetaDatas.Services.Service.Identifier"/>
    /// </summary>
    [JsonIgnore]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// 动态生成method
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => string.IsNullOrEmpty(ModuleId)
        ? $"thing.service.{ServiceId}"
        : $"thing.service.{ModuleId}:{ServiceId}";

    /// <summary>
    /// 服务调用参数
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
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
        if (string.IsNullOrWhiteSpace(ServiceId))
        {
            throw new ArgumentNullException(nameof(ServiceId), "ServiceId不能为空");
        }
        var serviceIdentifier = string.IsNullOrEmpty(ModuleId) ? ServiceId : $"{ModuleId}:{ServiceId}";
        return $"/sys/{productKey}/{deviceName}/thing/service/{serviceIdentifier}";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params == null || !Params.Any())
        {
            return ValidateResult.Failed("服务调用参数不能为空");
        }
        return ValidateResult.Success();
    }
}

