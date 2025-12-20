namespace Artizan.IoTHub.Mqtts.Options.Pollys;

/// <summary>
/// 熔断策略子配置
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// 触发熔断前允许的失败次数
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 10; // 失败10次后熔断

    /// <summary>
    /// 熔断持续时间（秒）
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30; // 熔断30秒
}
