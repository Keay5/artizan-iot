using Artizan.IoT.Mqtt.Messages.Dispatchers;
using System;

namespace Artizan.IoT.Mqtt.Options;

/// <summary>
/// MQTT消息分发器配置选项
/// </summary>
public class MqttMessageDispatcherOptions
{
    /// <summary>
    /// 分区数量（建议为CPU核心数的2-4倍）
    /// </summary>
    public int PartitionCount { get; set; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// 每个Channel的容量（限制内存占用，避免无限容量导致OOM（内存溢出））
    /// </summary>
    public int ChannelCapacity { get; set; } = 10000;

    /// <summary>
    /// 批量处理大小
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 批量超时时间
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// 分区策略（默认：设备有序，保证单设备消息时序）
    /// </summary>
    public MqttMessagePartitionStrategy PartitionStrategy { get; set; } = MqttMessagePartitionStrategy.DeviceOrdered;

    /// <summary>
    /// 【可选】是否自动生成消息ID（仅并行策略生效，默认：true）
    /// </summary>
    public bool AutoGenerateMessageId { get; set; } = true;
}
