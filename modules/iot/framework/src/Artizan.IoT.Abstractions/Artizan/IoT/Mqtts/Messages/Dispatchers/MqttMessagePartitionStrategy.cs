using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Dispatchers;

/// <summary>
/// MQTT消息分区策略枚举 （配置文件可直接写字符串如"DeviceOrdered"）
/// </summary>
public enum MqttMessagePartitionStrategy
{
    /// <summary>
    /// 按设备哈希分区（单设备消息严格有序）
    /// 按设备哈希分区（单设备消息严格有序，适合需保证时序的场景，如设备状态上报、指令下发）
    /// 特点：同一设备的所有消息分配到同一个分区，牺牲并行性保证顺序
    /// </summary>
    DeviceOrdered = 0,

    /// <summary>
    /// 按设备+消息ID哈希分区（单设备消息并行处理）
    /// 按设备+消息ID哈希分区（单设备消息可乱序，适合高并发、高吞吐场景，如海量设备日志、非时序数据）
    /// 特点：同一设备的消息分散到多个分区，充分利用并行能力，牺牲绝对顺序
    /// </summary>
    DeviceParallel = 1
}
