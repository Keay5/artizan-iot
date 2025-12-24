using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.Products.ProductMoudles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备事件上报请求（原有注释：设备向云端上报事件，如告警、状态变更）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services
/// 【Topic模板】：
///     -默认模块：   /sys/${productKey}/${deviceName}/thing/event/${eventIdentifier}/post
///     - 自定义模块：/sys/${productKey}/${deviceName}/thing/event/${moduleIdentifier}:${eventIdentifier}/post
/// 【Method】：
///     -默认模块：   thing.event.${eventIdentifier}.post 
///     - 自定义模块：thing.event.${moduleIdentifier}:${eventIdentifier}.post
/// 【模块规则】：
///     - 默认模块：无需填写ModuleIdentifier（关联ProductModule.Identifier="Default"、TSL.functionBlockId="Default"，不区分大小写）；
///     - 自定义模块：ModuleIdentifier需与ProductModule.Identifier/TSL.functionBlockId严格一致（区分大小写）；
///     - 禁止显式传入"Default"作为ModuleIdentifier。
/// </summary>
public class EventPostRequest : AlinkRequestBase
{
    /// <summary>
    /// 模块标识（原ModuleId，对应TSL的functionBlockId/ProductModule的Identifier）
    /// - 默认模块：无需填写（系统自动关联Identifier="Default"的ProductModule，不区分大小写）；
    /// - 自定义模块：需与ProductModule的Identifier严格一致（区分大小写）。
    /// </summary>
    [JsonIgnore]
    public string? ModuleIdentifier { get; set; }

    /// <summary>
    /// 事件标识（原EventId，对应TSL中定义的事件ID）
    /// </summary>
    [JsonIgnore]
    public string EventIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 动态生成method（原有注释：根据模块和事件标识拼接协议方法名）
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModuleIdentifier))
            {
                // 默认模块：关联Identifier=Default的ProductModule
                return $"thing.event.{EventIdentifier}.post";
            }
            // 自定义模块：拼接ModuleIdentifier（区分大小写）
            return $"thing.event.{ModuleIdentifier}:{EventIdentifier}.post";
        }
    }

    /// <summary>
    /// 事件参数（原有注释：key为事件参数ID，value为参数值+时间戳）
    /// </summary>
    [JsonPropertyName("params")]
    public AlinkEventParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic（原有注释：根据产品Key、设备Name、模块和事件标识生成上报Topic）
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
        if (string.IsNullOrWhiteSpace(EventIdentifier))
        {
            throw new ArgumentNullException(nameof(EventIdentifier), "EventIdentifier不能为空");
        }

        string eventIdentifier;
        if (string.IsNullOrWhiteSpace(ModuleIdentifier))
        {
            // 默认模块：直接使用事件标识
            eventIdentifier = EventIdentifier;
        }
        else
        {
            // 自定义模块：拼接ModuleIdentifier（区分大小写）
            eventIdentifier = $"{ModuleIdentifier}:{EventIdentifier}";
        }
        return $"/sys/{productKey}/{deviceName}/thing/event/{eventIdentifier}/post";
    }

    /// <summary>
    /// 校验参数（原有注释：校验事件标识非空、参数合法）
    /// 【补充规则】：禁止显式传入Default作为ModuleIdentifier（默认模块无需填写）
    /// </summary>
    public ValidateResult Validate()
    {
        // 校验ModuleIdentifier规则
        if (!string.IsNullOrWhiteSpace(ModuleIdentifier) && ProductModuleHelper.IsDefaultModule(ModuleIdentifier))
        {
            return ValidateResult.Failed("默认模块无需填写ModuleIdentifier，请勿显式传入\"Default\"");
        }

        if (string.IsNullOrWhiteSpace(EventIdentifier))
        {
            return ValidateResult.Failed("事件标识（EventIdentifier）不能为空");
        }
        if (Params.Time > DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeMilliseconds())
        {
            return ValidateResult.Failed("事件时间戳超出范围（仅允许未来24小时内）");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 事件参数
/// </summary>
public class AlinkEventParams
{
    [JsonPropertyName("value")]
    public Dictionary<string, object> Value { get; set; } = new();

    [JsonPropertyName("time")]
    public long? Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

