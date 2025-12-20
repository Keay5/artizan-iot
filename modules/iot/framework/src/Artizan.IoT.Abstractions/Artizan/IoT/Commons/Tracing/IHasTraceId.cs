namespace Artizan.IoT.Commons.Tracing;

/// <summary>
/// TraceId标记接口
/// 带追踪ID的通用接口（全链路追踪）
/// 【设计考量】：所有需要全链路追踪的核心类均实现此接口，统一TraceId管理
/// 【设计思路】
/// 1. 核心职责：统一标记需要携带全链路TraceId的对象（DTO/实体/结果），提升代码语义性；
/// 2. 设计原则：接口隔离（仅定义TraceId字段，无其他逻辑）、单一职责（聚焦TraceId标记）；
/// 3. 核心考量：
///    - 适配ABP社区版：不依赖任何商业包，仅基于.NET原生能力；
///    - 全链路复用：可用于MQTT消息、物模型数据、设备影子等所有需要追踪的场景；
///    - 扩展性：后续可扩展为泛型接口，兼容不同TraceId类型（如Guid/字符串）。
/// 【设计模式】：标记接口模式（Marker Interface Pattern）
/// - 模式说明：仅用于标记对象具备某类特性（携带TraceId），无方法定义，便于统一处理。
/// </summary>
public interface IHasTraceId
{
    /// <summary>
    /// 全链路追踪ID（唯一标识一条请求链路，用于日志排查/问题溯源）
    /// 【设计考量】：
    /// - 格式：默认采用GUID（无连字符），兼容大多数日志系统/监控工具；
    /// - 传递：贯穿协议解析、校验、路由、处理全流程，确保链路可追溯；
    /// - 非空：生成逻辑保证TraceId不为空，避免日志缺失关键标识。
    /// </summary>
    string TraceId { get; set; }
}
