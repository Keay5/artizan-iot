using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 设备期望属性值请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/desired-device-property-values
/// Method：thing.property.desired.set
/// </summary>
public class DesiredPropertySetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.property.desired.set";

    [JsonPropertyName("params")]
    public DesiredPropertyParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/property/desired/set";
    }
}

/// <summary>
/// 期望属性参数
/// </summary>
public class DesiredPropertyParams
{
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("expireTime")]
    public long? ExpireTime { get; set; }
}


