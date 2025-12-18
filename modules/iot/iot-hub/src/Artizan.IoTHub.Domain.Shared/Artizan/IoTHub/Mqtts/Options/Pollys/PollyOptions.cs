namespace Artizan.IoTHub.Mqtts.Options.Pollys;

/// <summary>
/// Polly 策略配置类（对应配置节点 PollyOptions）
/// </summary>
public class PollyOptions
{
    /// <summary>
    /// 熔断策略配置
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// 隔离策略配置
    /// </summary>
    public BulkheadOptions Bulkhead { get; set; } = new();

    /// <summary>
    /// 重试策略配置（新增）
    /// </summary>
    public RetryOptions Retry { get; set; } = new RetryOptions();
}
