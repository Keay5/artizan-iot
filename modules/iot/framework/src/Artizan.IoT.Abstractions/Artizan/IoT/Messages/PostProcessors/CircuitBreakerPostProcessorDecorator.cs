using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Settings;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 插件熔断装饰器（为插件添加熔断逻辑，不修改原插件）
/// 设计模式：装饰器模式，动态添加熔断功能，保持原插件纯净。
/// </summary>
/// <typeparam name="TContext">协议上下文类型</typeparam>
public class CircuitBreakerPostProcessorDecorator<TContext> : IMessagePostProcessor<TContext>
    where TContext : MessageContext
{
    private readonly IMessagePostProcessor<TContext> _innerProcessor;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly ISettingProvider _settingProvider;  //TODO:？使用SettingManager 才是最佳实践

    public int Priority => _innerProcessor.Priority;
    public bool IsEnabled => _innerProcessor.IsEnabled;

    public CircuitBreakerPostProcessorDecorator(
        IMessagePostProcessor<TContext> innerProcessor,
        ISettingProvider settingProvider)
    {
        _innerProcessor = innerProcessor;
        _settingProvider = settingProvider;
        // 初始化熔断策略（从配置读取参数）
        _circuitBreaker = CreateCircuitBreakerPolicy();
    }

    /// <summary>
    /// 执行插件逻辑并应用熔断
    /// </summary>
    public Task ProcessAsync(TContext context, CancellationToken cancellationToken = default)
    {
        // 用Polly熔断策略包装插件执行
        return _circuitBreaker.ExecuteAsync(
            () => _innerProcessor.ProcessAsync(context, cancellationToken));
    }

    /// <summary>
    /// 创建熔断策略（支持动态配置刷新）
    /// </summary>
    private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy()
    {
        var failureThreshold = 
            _settingProvider.GetAsync<int>($"{_innerProcessor.GetType().Name}:FailureThreshold").Result;
        var duration = 
            TimeSpan.FromSeconds(_settingProvider.GetAsync<int>($"{_innerProcessor.GetType().Name}:Duration").Result);

        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(failureThreshold, duration);
    }
}

