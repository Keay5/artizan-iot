using Artizan.IoT.Alinks.DataObjects.Commons;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.MessageCommunications;

/// <summary>
/// 期望属性获取响应
/// </summary>
public class DesiredPropertyGetResponse : AlinkResponseBase<DesiredPropertyGetData>
{
}

/// <summary>
/// 期望属性获取数据
/// </summary>
public class DesiredPropertyGetData
{
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
}