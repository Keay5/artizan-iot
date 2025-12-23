using System;

namespace Artizan.IoT.Tracing;

/// <summary>
/// TraceId上下文访问器接口（替代ABP商业版ICurrentTraceIdAccessor，适配社区版）
/// 【设计思路】
/// 1. 核心职责：提供TraceId的「当前上下文获取+临时修改」能力，适配异步场景；
/// 2. 设计原则：
///    - 接口隔离：仅定义核心能力（获取/修改），不绑定实现；
///    - 开闭原则：可替换不同实现（如AsyncLocal/HttpContext/Redis）；
/// 3. 核心考量：
///    - 异步安全：适配async/await场景，确保子线程/子任务的TraceId独立；
///    - 轻量高效：无额外依赖，基于.NET原生API实现；
///    - 易用性：Change方法返回IDisposable，using块自动恢复原TraceId，简化调用。
/// 【设计模式】：接口隔离模式（Interface Segregation Principle）
/// - 模式说明：将通用能力拆分为细粒度接口，调用方仅依赖所需方法，避免冗余依赖。
/// </summary>
public interface ICurrentTraceIdAccessor
{
    /// <summary>
    /// 获取当前上下文的TraceId
    /// </summary>
    string? Current { get; }

    /// <summary>
    /// 临时修改当前上下文的TraceId（using块结束后自动恢复原TraceId）
    /// 【设计考量】：
    /// - 异常安全：即使块内抛出异常，Dispose仍会执行，确保TraceId上下文不污染；
    /// - 场景适配：用于设备侧传递TraceId时，临时同步到上下文，后续流程复用。
    /// </summary>
    /// <param name="traceId">新的TraceId</param>
    /// <returns>IDisposable（释放时恢复原TraceId）</returns>
    IDisposable Change(string traceId);
}