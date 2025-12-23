using Microsoft.Extensions.Logging;
using Volo.Abp.Settings;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 熔断插件创建工具（简化手动包装逻辑）
/// </summary>
public static class MessagePostProcessorExtextions
{
    /// <summary>
    /// 创建带熔断的插件实例
    /// </summary>
    public static IMessagePostProcessor<TContext> CreateWithCircuitBreaker<TContext>(
        this IMessagePostProcessor<TContext> innerProcessor,
        ISettingProvider settingProvider,
        ILogger<CircuitBreakerPostProcessorDecorator<TContext>> logger)
        where TContext : MessageContext
    {
        return new CircuitBreakerPostProcessorDecorator<TContext>(innerProcessor, settingProvider, logger);
    }
}
