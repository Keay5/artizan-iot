namespace Artizan.IoTHub.Mqtts.Options.Pollys;

/// <summary>
/// 隔离策略子配置
/// </summary>
public class BulkheadOptions
{
    /// <summary>
    /// 最大并发处理数
    /// </summary>
    public int MaxParallelization { get; set; } = 100;

    /// <summary>
    /// 最大等待队列长度
    /// </summary>
    public int MaxQueuingActions { get; set; } = 1000;
}
