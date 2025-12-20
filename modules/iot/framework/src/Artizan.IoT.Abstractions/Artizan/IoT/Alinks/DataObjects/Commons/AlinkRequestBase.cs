using Artizan.IoT.Alinks.Serializers.Converters;
using Artizan.IoT.Commons.Tracing;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// Alink协议请求基类（所有Alink请求均继承此类）
/// 【协议共性】：id、version为必填，method为场景固定值
/// 【设计考量】：
/// 1. 继承IHasTraceId实现全链路追踪；
/// 2. id默认生成符合0~4294967295范围的随机数，降低设备端适配成本；
/// 3. version固定为1.0，强制遵循阿里云协议规范；
/// 4. 抽象Method属性，子类必须实现，确保每个场景绑定固定method。
/// </summary>
public abstract class AlinkRequestBase : IHasTraceId
{
    /// <summary>
    /// 全链路追踪ID（与系统追踪体系对齐）
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 消息ID（String类型数字，取值0~4294967295，设备端当日唯一）
    /// 【协议约束】：必填，且在当前设备内每天唯一,必须由于设备上传
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(LongFromStringOrNumberConverter))] // 应用自定义转换器,当 JSON 中 id 是字符串（如 "123"）：自动解析为 long。
    public long Id { get; set; } //= Random.Shared.Next(0, 4294967295).ToString();

    /// <summary>
    /// 协议版本（固定为1.0，不可修改）
    /// 【协议约束】：目前所有Alink JSON格式均使用1.0版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 请求方法（每个场景固定值，如"thing.event.property.post"）
    /// </summary>
    [JsonPropertyName("method")]
    public abstract string Method { get; }

    /// <summary>
    /// 扩展配置（可选，如ack字段控制是否需要响应）
    /// </summary>
    [JsonPropertyName("sys")]
    public AlinkSysConfig? Sys { get; set; }
}
