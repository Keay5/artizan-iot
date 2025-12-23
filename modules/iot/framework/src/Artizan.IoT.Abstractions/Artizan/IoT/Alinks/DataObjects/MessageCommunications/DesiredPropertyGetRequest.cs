using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 期望属性获取请求
/// Method：thing.property.desired.get
/// </summary>
public class DesiredPropertyGetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.property.desired.get";

    [JsonPropertyName("params")]
    public DesiredPropertyGetParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/property/desired/get";
    }
}
/// <summary>
/// 期望属性获取参数
/// </summary>
public class DesiredPropertyGetParams
{
    [JsonPropertyName("keys")]
    public List<string> Keys { get; set; } = new();
}

