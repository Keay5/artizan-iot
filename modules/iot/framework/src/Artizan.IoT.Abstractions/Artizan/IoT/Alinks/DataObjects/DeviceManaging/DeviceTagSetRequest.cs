using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备标签设置请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-tags
/// Method：thing.device.tags.set
/// </summary>
public class DeviceTagSetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.device.tags.set";

    [JsonPropertyName("params")]
    public DeviceTagParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/device/tags/set";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params.Tags == null || !Params.Tags.Any())
        {
            return ValidateResult.Failed("标签列表不能为空");
        }
        if (Params.Tags.Count > 100)
        {
            return ValidateResult.Failed("单次最多设置100个标签");
        }
        foreach (var tag in Params.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Key))
            {
                return ValidateResult.Failed("标签Key不能为空");
            }
        }
        return ValidateResult.Success();
    }
}






