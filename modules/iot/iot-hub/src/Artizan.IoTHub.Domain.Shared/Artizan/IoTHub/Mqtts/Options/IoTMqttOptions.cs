using Artizan.IoTHub.Mqtts.Options.Pollys;

namespace Artizan.IoTHub.Mqtts.Options;

/// <summary>
/// IoT MQTT核心配置选项（包含Polly和发布优化配置）
/// ----------------------------------
/// 生产环境调优建议
/// 批量阈值建议根据服务器性能调整（100-500 条 / 批）；
/// 隔离策略最大并发数建议设置为 CPU 核心数 × 1000；
/// 限流配置需根据实际业务热点主题调整；
/// 建议配合监控（如 Prometheus）观察批量发布成功率、熔断触发次数等指标。
/// -----------------------------------------
/// 配置示例：
///  "IoTMqtt": {
///    "Polly": {
///      "CircuitBreaker": {
///        "ExceptionsAllowedBeforeBreaking": 10, // 失败10次后熔断
///        "DurationOfBreakSeconds": 30 // 熔断时长（单位秒）
///      },
///      "Bulkhead": {
///        "MaxParallelization": 10000, // 最大并发数
///        "MaxQueuingActions": 1000 // 最大等待队列数
///      },
///      "Retry": { //重试策略配置
///        "RetryIntervalSeconds": 5, // 重试间隔5秒
///        "MaxRetryPerLoop": 100 // 每次循环最多重试100条消息
///      }
///    },
///    "PublishingOptimization": {
///      "EnableOptimizations": true,
///      "BatchPublishThreshold": 10,
///      "BatchPublishIntervalMs": 100,
///      "SingleMessagePublishTimeoutMs": 200, // 300ms既保证了高并发段的批量效率，又控制了低并发段的延迟在业务阈值内，是混合场景的“黄金平衡点”
///      "TopicBasedThrottling": { }
///    }
///  }
/// 
/// </summary>
public class IoTMqttOptions
{
    /// <summary>
    /// Polly熔断/隔离/重试配置
    /// </summary>
    public PollyOptions Polly { get; set; } = new PollyOptions();

    /// <summary>
    /// 发布优化配置
    /// </summary>
    public PublishingOptimizationOptions PublishingOptimization { get; set; } = new PublishingOptimizationOptions();
}
