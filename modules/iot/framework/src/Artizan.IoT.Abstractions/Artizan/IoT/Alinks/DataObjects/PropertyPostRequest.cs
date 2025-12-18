using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 设备上报属性请求（Alink JSON格式）
/// Topic: /sys/${productKey}/${deviceName}/thing/event/property/post
/// </summary>
public class PropertyPostRequest : AlinkRequestBase
{
    /// <summary>
    /// 扩展配置（可选）
    /// </summary>
    [JsonPropertyName("sys")]
    public AlinkSysConfig? Sys { get; set; }

    /// <summary>
    /// 上报的属性集合
    /// Key：属性标识符（自定义模块格式：${模块ID}:${属性ID}）
    /// Value：属性值（float/double需带小数位，如10.0）
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// 请求方法（固定值）
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "thing.event.property.post";
}
