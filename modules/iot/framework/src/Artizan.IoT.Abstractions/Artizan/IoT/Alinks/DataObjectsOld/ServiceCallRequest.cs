using Artizan.IoT.Alinks.DataObjectsOld;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// 云端异步调用设备服务请求（下行）
/// Topic: /sys/${productKey}/${deviceName}/thing/service/${serviceId}
/// </summary>
public class ServiceCallRequest : AlinkRequestBase
{
    /// <summary>
    /// 服务调用参数（与物模型定义一致）
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    /// <summary>
    /// 请求方法（格式：thing.service.${serviceId}）
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}
