using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 云端设置设备属性请求（下行）
/// Topic: /sys/${productKey}/${deviceName}/thing/service/property/set
/// </summary>
public class PropertySetRequest : AlinkRequestBase
{
    /// <summary>
    /// 待设置的属性集合
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// 请求方法（固定值）
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "thing.service.property.set";
}
