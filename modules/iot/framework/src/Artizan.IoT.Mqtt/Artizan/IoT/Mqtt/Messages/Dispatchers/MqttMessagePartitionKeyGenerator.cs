using Artizan.IoT.Mqtt.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Messages.Dispatchers;

/// <summary>
/// MQTT消息分区Key生成工具类（整合配置项）
/// </summary>
public class MqttMessagePartitionKeyGenerator : ISingletonDependency
{
    // 直接持有配置项，无需单独传分区数/策略
    private readonly MqttMessageDispatcherOptions _options;

    /// <summary>
    /// 构造函数（注入配置项）
    /// </summary>
    /// <param name="options">消息分发器配置</param>
    public MqttMessagePartitionKeyGenerator(IOptions<MqttMessageDispatcherOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (_options.PartitionCount <= 0)
        {
            throw new ArgumentException("分区数量必须大于0", nameof(options));
        }
    }

    #region 对外统一方法（自动读取配置的分区策略）
    /// <summary>
    /// 通用分区Key获取方法（自动使用配置的分区策略）
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="messageId">【可选】消息唯一ID（仅并行策略生效，配置AutoGenerateMessageId=true时可省略）</param>
    /// <returns>分区Key</returns>
    public string GetPartitionKey(string productKey, string deviceName, string messageId = null)
    {
        return _options.PartitionStrategy switch
        {
            MqttMessagePartitionStrategy.DeviceOrdered => GetDeviceOrderedPartitionKey(productKey, deviceName),
            MqttMessagePartitionStrategy.DeviceParallel => GetDeviceParallelPartitionKey(productKey, deviceName, messageId),
            _ => throw new NotSupportedException($"不支持的分区策略：{_options.PartitionStrategy}")
        };
    }

    /// <summary>
    /// 重载：手动指定策略（覆盖配置，适用于特殊场景）
    /// </summary>
    public string GetPartitionKey(string productKey, string deviceName,
                                 MqttMessagePartitionStrategy strategy, string messageId = null)
    {
        return strategy switch
        {
            MqttMessagePartitionStrategy.DeviceOrdered => GetDeviceOrderedPartitionKey(productKey, deviceName),
            MqttMessagePartitionStrategy.DeviceParallel => GetDeviceParallelPartitionKey(productKey, deviceName, messageId),
            _ => throw new NotSupportedException($"不支持的分区策略：{strategy}")
        };
    }
    #endregion

    #region 场景1：按设备哈希（严格有序）
    /// <summary>
    /// 获取「设备有序」分区Key（单设备消息严格按顺序处理）
    /// 【使用场景】：需保证同一设备消息时序的业务，如设备指令下发回执、状态变更流水、告警时序等
    /// 【缺点】：单设备高并发时，该分区会成为性能瓶颈，TPS受限
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="deviceName">设备名称</param>
    /// <returns>分区Key（数字字符串，范围：0~PartitionCount-1）</returns>
    private string GetDeviceOrderedPartitionKey(string productKey, string deviceName)
    {
        // 边界处理：空值设备分配到默认分区
        if (string.IsNullOrEmpty(productKey) || string.IsNullOrEmpty(deviceName))
        {
            return "0"; // 默认分区固定为0
        }

        // 按设备唯一标识哈希取模，保证同一设备固定分区
        var deviceUniqueKey = $"{productKey}:{deviceName}";
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(deviceUniqueKey);
        // 处理哈希负数（GetHashCode可能返回负数）
        var partitionIndex = (hash % _options.PartitionCount + _options.PartitionCount) % _options.PartitionCount;
        return partitionIndex.ToString();
    }
    #endregion

    #region 场景2：按设备+消息ID哈希（高并行）
    /// <summary>
    /// 获取「设备并行」分区Key（单设备消息分散到多分区并行处理）
    /// 【使用场景】：无需保证同一设备消息顺序的高吞吐业务，如设备原始日志、温湿度采集数据、非时序统计数据等
    /// 【优点】：充分利用多分区并行能力，TPS提升至接近分区数倍
    /// 【缺点】：同一设备的消息可能乱序，无法保证时序
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="messageId">消息唯一ID（为空则自动生成Guid，保证打散效果）</param>
    /// <returns>分区Key（数字字符串，范围：0~PartitionCount-1）</returns>
    private string GetDeviceParallelPartitionKey(string productKey, string deviceName, string messageId)
    {
        // 边界处理：空值设备分配到默认分区
        if (string.IsNullOrEmpty(productKey) || string.IsNullOrEmpty(deviceName))
        {
            return "0"; // 默认分区固定为0
        }

        // 基础设备Key哈希
        var deviceUniqueKey = $"{productKey}:{deviceName}";
        var deviceHash = StringComparer.OrdinalIgnoreCase.GetHashCode(deviceUniqueKey);

        // 读取配置：是否自动生成消息ID
        // 消息ID哈希（打散同一设备的消息），为空则用Guid兜底
        var msgUniqueKey = !string.IsNullOrEmpty(messageId)
            ? messageId
            : (_options.AutoGenerateMessageId ? Guid.NewGuid().ToString("N") : "default_msg");

        var msgHash = StringComparer.OrdinalIgnoreCase.GetHashCode(msgUniqueKey);

        // 混合哈希（异或）+ 取模，保证分散均匀且避免负数
        var mixHash = deviceHash ^ msgHash;
        var partitionIndex = (mixHash % _options.PartitionCount + _options.PartitionCount) % _options.PartitionCount;
        return partitionIndex.ToString();
    }
    #endregion
}
