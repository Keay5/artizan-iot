namespace Artizan.IoTHub.Mqtts.Messages.PostProcessors;

/// <summary>
/// 集中管理内置的 PostProcessor 的执行优先级 Priority
/// 数字越小越先执行
/// </summary>
public static class MqttMessagePostProcessorPriorityConstants
{
    /*
     有新增的内置 Mqtt Message PostProcessor，统一在这里配置优先级
     */

    /// <summary>
    /// 【设计考量】
    /// - 优先级50：确保缓存处理器在消息解析、验证等前置处理器之后执行
    /// - 避免处理未解析/验证失败的数据，保证缓存数据的有效性
    /// </summary>
    public const int MqttCacheMessagePostProcessorPriority = 50;
    public const int MqttCacheHistoryMessagePostProcessorPriority = 51;
}

