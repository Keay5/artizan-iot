using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Dispatchers;

/// <summary>
/// 分发器配置选项
/// </summary>
public class MqttDispatcherOptions
{
    /// <summary>
    /// 分区数量（建议为CPU核心数的2-4倍）
    /// </summary>
    public int PartitionCount { get; set; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// 每个Channel的容量（限制内存占用，避免无限容量导致OOM（内存溢出））
    /// </summary>
    public int ChannelCapacity { get; set; } = 100000;
}
