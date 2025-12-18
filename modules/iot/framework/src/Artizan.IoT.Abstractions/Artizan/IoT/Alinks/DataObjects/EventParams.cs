using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// 事件参数（包含输出参数和时间戳）
/// </summary>
public class EventParams
{
    /// <summary>
    /// 事件输出参数（与物模型定义一致）
    /// </summary>
    [JsonPropertyName("value")]
    public Dictionary<string, object> Value { get; set; } = new();

    /// <summary>
    /// 事件发生时间（UTC毫秒级，可选，不传则云端自动生成）
    /// </summary>
    [JsonPropertyName("time")]
    public long? Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
