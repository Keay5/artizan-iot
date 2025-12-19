using System.Collections.Generic;

namespace Artizan.IoTHub.Mqtts.Options;

/// <summary>
/// 发布服务优化配置选项
/// ---------------------------------------------
/// 针对高并发场景，建议调整 PublishingOptimization 配置
/// "PublishingOptimization": {
///    "EnableOptimizations": true,
///    "BatchPublishThreshold": 200, // 每批200条，减少调用次数
///    "BatchPublishIntervalMs": 50, // 50ms检查一次，降低延迟
///    "TopicBasedThrottling": {
///      "device/data/": 10000 // 热点主题限流
///    }
///  }
/// 
/// </summary>
public class PublishingOptimizationOptions
{
    /// <summary>
    /// 是否启用优化功能
    /// </summary>
    public bool EnableOptimizations { get; set; } = false;

    /// <summary>
    /// 批量发布阈值（达到此数量时触发批量发布）
    /// </summary>
    public int BatchPublishThreshold { get; set; } = 100;

    /// <summary>
    /// 批量发布时间间隔（毫秒）
    /// </summary>
    public int BatchPublishIntervalMs { get; set; } = 100;

    /// <summary>
    /// 用于控制单条消息的最大等待时间（毫秒）
    /// 200ms（适配 80% 的 IoT / 高并发场景，兼顾批量性能和单条消息延迟）
    /// 300ms既保证了高并发段的批量效率，又控制了低并发段的延迟在业务阈值内，是混合场景的“黄金平衡点”
    /// </summary>
    public int SingleMessagePublishTimeoutMs { get; set; } = 300;

    /// <summary>
    /// 基于主题的限流配置（主题前缀 -> 每秒最大消息数）
    /// </summary>
    public Dictionary<string, int> TopicBasedThrottling { get; set; } = new Dictionary<string, int>();
}
