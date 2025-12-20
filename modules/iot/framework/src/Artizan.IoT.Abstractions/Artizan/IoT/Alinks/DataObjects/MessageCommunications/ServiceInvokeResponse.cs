using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 服务调用响应
/// </summary>
public class ServiceInvokeResponse : AlinkResponseBase<ServiceInvokeResponseData>
{
}

/// <summary>
/// 服务调用响应数据
/// </summary>
public class ServiceInvokeResponseData
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("outputParams")]
    public Dictionary<string, object> OutputParams { get; set; } = new();

    [JsonPropertyName("executeTime")]
    public long ExecuteTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
