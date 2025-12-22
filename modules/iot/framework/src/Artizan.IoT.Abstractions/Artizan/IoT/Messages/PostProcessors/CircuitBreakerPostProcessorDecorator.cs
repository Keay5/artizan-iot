using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 插件熔断装饰器（为插件添加熔断逻辑，不修改原插件）
/// 设计模式：装饰器模式，动态添加熔断功能，保持原插件纯净。
/// </summary>
/// <typeparam name="TContext">协议上下文类型</typeparam>
public class CircuitBreakerPostProcessorDecorator<TContext> : IMessagePostProcessor<TContext>, ITransientDependency
    where TContext : MessageContext
{
    private readonly ILogger<CircuitBreakerPostProcessorDecorator<TContext>> _logger;
    private readonly ISettingProvider _settingProvider;

    private readonly IMessagePostProcessor<TContext> _innerProcessor;
    private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly SemaphoreSlim _policyLock = new(1, 1);

    public int Priority => _innerProcessor.Priority;
    public bool IsEnabled => _innerProcessor.IsEnabled;

    public CircuitBreakerPostProcessorDecorator(
        IMessagePostProcessor<TContext> innerProcessor,
        ISettingProvider settingProvider,
        ILogger<CircuitBreakerPostProcessorDecorator<TContext>> logger
      )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _innerProcessor = innerProcessor ?? throw new ArgumentNullException(nameof(innerProcessor));
        _settingProvider = settingProvider ?? throw new ArgumentNullException(nameof(settingProvider));
        _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
    }

    /// <summary>
    /// 执行插件逻辑并应用熔断
    /// </summary>
    public async Task ProcessAsync(TContext context, CancellationToken cancellationToken = default)
    {

        await RefreshCircuitBreakerPolicyAsync();
        try
        {
            // 用Polly熔断策略包装插件执行
            await _circuitBreakerPolicy.ExecuteAsync(() =>
                _innerProcessor.ProcessAsync(context, cancellationToken));
        }
        catch (BrokenCircuitException ex)
        {
            var errorMsg = $"[{context.TraceId}] 插件{_innerProcessor.GetType().Name}熔断触发：{ex.Message}";
            _logger.LogWarning(errorMsg);

            //TODO: 基类添加此方法？
            //context.UpdateStepResult(
            //    stepName: $"CircuitBreaker:{_innerProcessor.GetType().Name}",
            //    isSuccess: false,
            //    elapsed: TimeSpan.Zero,
            //    errorMsg: errorMsg,
            //    exception: ex
            //);
        }
    }

    ///// <summary>
    ///// 创建熔断策略（支持动态配置刷新）
    ///// </summary>
    //private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy()
    //{
    //    var failureThreshold =
    //        _settingProvider.GetAsync<int>($"{_innerProcessor.GetType().Name}:FailureThreshold").Result;
    //    var duration =
    //        TimeSpan.FromSeconds(_settingProvider.GetAsync<int>($"{_innerProcessor.GetType().Name}:Duration").Result);

    //    return Policy
    //        .Handle<Exception>()
    //        .CircuitBreakerAsync(failureThreshold, duration);
    //}

    ///// <summary>
    ///// 创建熔断策略（支持动态配置刷新）
    ///// </summary>
    private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy()
    {
        var processorName = _innerProcessor.GetType().Name;
        var failureThreshold = int.Parse(_settingProvider.GetOrNullAsync($"CircuitBreaker:{processorName}:FailureThreshold").Result ?? "0.5");
        var durationSeconds = int.Parse(_settingProvider.GetOrNullAsync($"CircuitBreaker:{processorName}:DurationSeconds").Result ?? "30");

        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                onBreak: (ex, breakDuration) => _logger.LogWarning($"消息处理插件「{processorName}」熔断 | 时长：{breakDuration.TotalSeconds}秒 | 异常：{ex.Message}"),
                onReset: () => _logger.LogInformation($"消息处理插件「{processorName}」熔断重置"),
                onHalfOpen: () => _logger.LogInformation($"消息处理插件「{processorName}」半开状态")
            );
    }

    private async Task RefreshCircuitBreakerPolicyAsync()
    {
        await _policyLock.WaitAsync();
        try
        {
            _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        }
        finally
        {
            _policyLock.Release();
        }
    }
}

