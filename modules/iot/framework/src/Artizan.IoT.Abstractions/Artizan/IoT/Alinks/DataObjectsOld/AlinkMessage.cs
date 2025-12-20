using Artizan.IoT.Abstractions.Commons.Tracing;
using Artizan.IoT.Commons.Tracing;
using System;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// 平台标准化MQTT消息DTO（Data Transfer Object设计模式）
/// 【设计思路】
/// 1. 核心职责：统一设备上报的AlinkJson消息格式，解耦协议解析层与后续业务处理层，避免直接依赖原始JSON结构
/// 2. 设计原则：
///    - 单一职责：仅承载标准化后的消息数据，无业务逻辑
///    - 接口隔离：实现IHasTraceId接口，适配框架的全链路追踪能力
/// 3. 关键考量：
///    - 保留原始消息/Topic字段，用于异常排查和问题回溯
///    - ModuleCode默认值为"default"，兼容无模块划分的简单产品
///    - TraceId自动生成，优先复用AlinkJson中的id字段，确保全链路唯一标识
/// </summary>
public class AlinkMessage : IHasTraceId
{
    /// <summary>
    /// 全链路追踪ID（核心设计：贯穿协议解析、校验、路由、处理全流程，用于问题溯源）
    /// 【设计考量】
    /// - 默认生成GUID（无连字符），确保唯一性；优先读取AlinkJson的id字段，兼容设备侧传递的追踪ID
    /// - 遵循ABP框架的IHasTraceId规范，可无缝集成分布式追踪系统（如Jaeger/Zipkin）
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 产品Key（IoT平台产品唯一标识，关联ProductModule和TSL）
    /// 【设计考量】：作为核心维度，用于后续TSL缓存查询、设备归属校验、权限控制
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（产品内设备唯一标识）
    /// 【设计考量】：与ProductKey组合构成设备全局唯一标识，用于设备影子、缓存Key生成
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 物模型操作方法（如：thing.event.property.post - 属性上报、thing.event.overtemp.post - 事件上报）
    /// 【设计考量】：作为路由和TSL校验的核心依据，遵循AlinkJson标准方法命名规范
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 物模型参数（JSON字符串，需符合对应ProductModule的TSL Schema）
    /// 【设计考量】：统一存储为JSON字符串，避免强类型绑定，兼容不同物模型的参数结构
    /// </summary>
    public string Params { get; set; } = string.Empty;

    /// <summary>
    /// 消息时间戳（毫秒级）
    /// 【设计考量】：优先读取设备上报的timestamp，无则使用平台接收时间，确保时序数据的准确性
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 原始MQTT消息内容
    /// 【设计考量】：解析/校验失败时，保留原始消息用于问题复现和排查，是可观测性的核心字段
    /// </summary>
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>
    /// 原始MQTT Topic
    /// 【设计考量】：用于提取ModuleIdentifier、路由规则匹配、权限校验，保留原始值避免信息丢失
    /// </summary>
    public string RawTopic { get; set; } = string.Empty;

    /// <summary>
    /// 无参构造函数（适配编译期语法糖兼容场景）
    /// 【设计考量】：可选参数在IL层面不生成单参数构造，补充无参构造避免编译报错
    /// </summary>
    public AlinkMessage()
    {
        TraceId = Guid.NewGuid().ToString("N");
        ProductKey = string.Empty;
        DeviceName = string.Empty;
        Method = string.Empty;
        Params = string.Empty;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RawMessage = string.Empty;
        RawTopic = string.Empty;
    }

    /// <summary>
    /// 构造函数（适配ABP社区版DI，可选注入TraceId访问器）
    /// 【设计思路】：
    /// - 可选注入：无DI场景（如单元测试）也能正常初始化；
    /// - 优先级：上下文TraceId > 自动生成GUID，确保链路一致性。
    /// </summary>
    /// <param name="traceIdAccessor">自定义TraceId访问器（ABP DI自动注入）</param>
    public AlinkMessage(ICurrentTraceIdAccessor? traceIdAccessor = null)
    {
        TraceId = traceIdAccessor?.Current ?? Guid.NewGuid().ToString("N");
        // 保留原有默认值赋值逻辑（与无参构造对齐）
        ProductKey = string.Empty;
        DeviceName = string.Empty;
        Method = string.Empty;
        Params = string.Empty;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RawMessage = string.Empty;
        RawTopic = string.Empty;
    }
}


