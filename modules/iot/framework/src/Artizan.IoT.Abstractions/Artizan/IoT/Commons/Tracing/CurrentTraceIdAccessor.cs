using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Abstractions.Commons.Tracing;

/// <summary>
/// TraceId上下文访问器实现（基于.NET AsyncLocal，ABP社区版适配）
/// 【设计思路】
/// 1. 核心实现：使用AsyncLocal<T>存储TraceId，确保异步上下文（async/await）中TraceId不丢失；
/// 2. 生命周期：Scoped（请求级），适配ABP社区版DI容器，每个请求独立上下文；
/// 3. 容错设计：
///    - Change方法返回Disposable对象，确保异常场景下也能恢复原TraceId；
///    - AsyncLocal默认值为null，取值时做空值处理，避免空引用。
/// 【核心技术】：AsyncLocal<T>
/// - 优势：.NET原生API，专为异步上下文设计，性能无损耗，无需额外依赖；
/// - 场景：适配MQTT消息处理的异步流程（解析→校验→处理）。
/// 【设计模式】：单例模式（Scoped单例）+ 封装模式
/// - Scoped单例：每个请求一个实例，避免多请求TraceId冲突；
/// - 封装模式：将AsyncLocal的复杂操作封装为简单接口，调用方无需关注底层实现。
/// </summary>
//推荐使用** 单例模式**（移除 Scoped 标记，保留`ISingletonDependency`），因为 `AsyncLocal` 天然支持异步上下文隔离，单例更高效；
//[Dependency(ServiceLifetime.Scoped)] 
public class CurrentTraceIdAccessor : ICurrentTraceIdAccessor, ISingletonDependency
{
    /// <summary>
    /// AsyncLocal存储TraceId（核心：确保异步上下文传递）
    /// `AsyncLocal` 的上下文隔离是**基于异步调用链**的，而非组件实例本身 
    /// —— 即使 `CurrentTraceIdAccessor` 是单例（Singleton），`AsyncLocal` 也能保证不同请求 / 异步链的 TraceId 相互隔离。
    /// </summary>
    private readonly AsyncLocal<string?> _currentTraceId = new();

    /// <summary>
    /// 获取当前上下文的TraceId
    /// </summary>
    public string? Current => _currentTraceId.Value;

    /// <summary>
    /// 临时修改TraceId上下文
    /// </summary>
    public IDisposable Change(string traceId)
    {
        // 保存原TraceId，用于后续恢复
        var originalTraceId = _currentTraceId.Value;
        // 设置新TraceId
        _currentTraceId.Value = traceId;

        // 返回自定义Disposable，释放时恢复原TraceId
        return new DisposableAction(() =>
        {
            _currentTraceId.Value = originalTraceId;
        });
    }

    /// <summary>
    /// 内部Disposable实现（封装恢复逻辑）
    /// 【设计思路】：将恢复逻辑封装为独立类，简化Change方法，符合单一职责。
    /// </summary>
    private class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private bool _disposed; // 防止重复释放

        public DisposableAction(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action), "恢复TraceId的Action不能为空");
        }

        public void Dispose()
        {
            if (_disposed) return;

            _action.Invoke();
            _disposed = true;
        }
    }
}