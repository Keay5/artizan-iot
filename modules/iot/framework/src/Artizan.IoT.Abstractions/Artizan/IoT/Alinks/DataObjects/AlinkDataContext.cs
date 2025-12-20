using Artizan.IoT.Abstractions.Commons.Tracing;
using Artizan.IoT.Alinks.DataObjects.Commons;
using Artizan.IoT.Commons.Tracing;
using System;

namespace Artizan.IoT.Alinks.DataObjects;

/// <summary>
/// Alink协议数据上下文（承载协议全生命周期数据）
/// 【设计理念】：
/// 1. 聚合所有相关数据，简化流程参数传递；
/// 2. 包含原始消息，便于异常排查；
/// 3. 扩展字段支持业务自定义数据；
/// 4. 继承IHasTraceId，统一全链路追踪。
/// </summary>
public class AlinkDataContext : IHasTraceId
{
    /// <summary>
    /// 全链路TraceId（与通用追踪体系对齐）
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 原始MQTT消息（Topic+Payload），便于排查
    /// </summary>
    public MqttRawMessage RawMessage { get; set; } = new();

    /// <summary>
    /// Alink请求对象（设备→云端场景）
    /// </summary>
    public AlinkRequestBase? Request { get; set; }

    /// <summary>
    /// Alink响应对象（云端→设备场景）
    /// </summary>
    public AlinkResponseBase? Response { get; set; }

    /// <summary>
    /// 业务扩展字段容器（遵循开放封闭原则）
    /// </summary>
    public AlinkContextExtension Extension { get; } = new();

    /// <summary>
    /// 处理结果（成功/失败，错误信息）
    /// </summary>
    public ValidateResult HandleResult { get; set; } = ValidateResult.Success();

    /// <summary>
    /// 处理完成时间（UTC）
    /// </summary>
    public DateTimeOffset HandleTime { get; set; }


    /// <summary>
    /// 无参构造函数（适配编译期语法糖兼容场景）
    /// 【设计考量】：可选参数在IL层面不生成单参数构造，补充无参构造避免编译报错
    /// </summary>
    private AlinkDataContext()
    { 
    }

    /// <summary>
    /// 构造函数（适配ABP社区版DI，可选注入TraceId访问器）
    /// 【设计思路】：
    /// - 可选注入：无DI场景（如单元测试）也能正常初始化；
    /// - 优先级：上下文TraceId > 自动生成GUID，确保链路一致性。
    /// </summary>
    /// <param name="traceIdAccessor">自定义TraceId访问器（ABP DI自动注入）</param>
    public AlinkDataContext(ICurrentTraceIdAccessor? traceIdAccessor = null)
    {
        TraceId = traceIdAccessor?.Current ?? Guid.NewGuid().ToString("N");
    }
}