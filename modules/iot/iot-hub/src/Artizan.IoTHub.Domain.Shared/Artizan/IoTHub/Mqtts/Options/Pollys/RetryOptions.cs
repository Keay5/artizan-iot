namespace Artizan.IoTHub.Mqtts.Options.Pollys;

/// <summary>
/// 重试策略配置类
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// 重试间隔（秒）
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 每次循环最大重试数量
    /// </summary>
    public int MaxRetryPerLoop { get; set; } = 100;
}
