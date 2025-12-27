using Artizan.IoT.Tracing;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// Alink协议响应基类（所有云端响应均继承此类）
/// 【协议共性】：id（与请求一致）、code、message、version、method、data
/// 【设计考量】：
/// 1. 继承IHasTraceId实现全链路追踪；
/// 2. 泛型Data设计，避免object强转，提升类型安全；
/// 3. Code默认200（成功），Message默认"success"，简化成功响应构造；
/// 4. 严格对齐阿里云错误码规范，确保响应可解析。
/// </summary>
public abstract class AlinkResponseBase<TData> : IHasTraceId
{
    /// <summary>
    /// 全链路追踪ID（与请求TraceId一致）
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 消息ID（与请求ID一致，确保链路可追溯）
    /// 【协议约束】：必须与请求id相同
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 响应码（200=成功，其他参考阿里云IoT错误码）
    /// 【协议约束】：严格遵循阿里云错误码规范
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; } = 200;

    /// <summary>
    /// 响应描述（成功为"success"，失败为具体错误信息）
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "success";

    /// <summary>
    /// 协议版本（固定为1.0）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 对应请求的method（确保响应与请求场景匹配）
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 响应业务数据（泛型设计，子类指定具体类型）
    /// </summary>
    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}

/// <summary>
/// 无业务数据的响应基类（简化空Data场景）
/// </summary>
public class AlinkResponseBase : AlinkResponseBase<object>
{
}