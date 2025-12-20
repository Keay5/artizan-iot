using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备标签获取请求
/// Method：thing.device.tags.get
/// </summary>
public class DeviceTagGetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.device.tags.get";

    [JsonPropertyName("params")]
    public DeviceTagGetParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/device/tags/get";
    }
}

/// <summary>
/// 标签获取参数
/// </summary>
public class DeviceTagGetParams
{
    [JsonPropertyName("keys")]
    public List<string> Keys { get; set; } = new(); // 为空则获取所有标签
}

