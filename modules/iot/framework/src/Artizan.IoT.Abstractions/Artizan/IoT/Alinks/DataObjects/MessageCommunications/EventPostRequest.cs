using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备事件上报请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services
/// Topic模板（默认模块）：/sys/${productKey}/${deviceName}/thing/event/${eventId}/post
/// Topic模板（自定义模块）：/sys/${productKey}/${deviceName}/thing/event/${moduleId}:${eventId}/post
/// Method：thing.event.${eventId}.post / thing.event.${moduleId}:${eventId}.post
/// </summary>
public class EventPostRequest : AlinkRequestBase
{
    /// <summary>
    /// 模块ID（自定义模块必填）
    /// </summary>
    [JsonIgnore]
    public string? ModuleId { get; set; }

    /// <summary>
    /// 事件ID
    /// </summary>
    [JsonIgnore]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 动态生成method
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => string.IsNullOrEmpty(ModuleId)
        ? $"thing.event.{EventId}.post"
        : $"thing.event.{ModuleId}:{EventId}.post";

    /// <summary>
    /// 事件参数
    /// </summary>
    [JsonPropertyName("params")]
    public AlinkEventParams Params { get; set; } = new();

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
        if (string.IsNullOrWhiteSpace(EventId))
        {
            throw new ArgumentNullException(nameof(EventId), "EventId不能为空");
        }
        var eventIdentifier = string.IsNullOrEmpty(ModuleId) ? EventId : $"{ModuleId}:{EventId}";
        return $"/sys/{productKey}/{deviceName}/thing/event/{eventIdentifier}/post";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params.Value == null || !Params.Value.Any())
        {
            return ValidateResult.Failed("事件参数不能为空");
        }
        if (Params.Time.HasValue)
        {
            var time = DateTimeOffset.FromUnixTimeMilliseconds(Params.Time.Value);
            var maxTime = DateTimeOffset.UtcNow.AddHours(24);
            if (time > maxTime)
            {
                return ValidateResult.Failed("事件time超出范围（仅允许未来24小时内）");
            }
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

