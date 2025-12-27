using System;

namespace Artizan.IoT.BatchProcessing.Configurations;

/// <summary>
/// 分区健康检查配置
/// 【设计思路】：监控配置独立，便于运维和告警配置
/// 【设计考量】：
/// 1. 多维度阈值配置（队列长度/失败率/处理耗时），全面监控分区状态
/// 2. 支持多种告警类型，适配不同运维场景
/// 【设计模式】：POCO + 配置绑定
/// </summary>
public class PartitionHealthOptions
{
    /// <summary>
    /// 健康检查间隔（默认30秒）
    /// </summary>
    public TimeSpan? CheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 队列长度告警阈值（默认5000）
    /// </summary>
    public int? QueueLengthWarnThreshold { get; set; } = 5000;

    /// <summary>
    /// 失败率告警阈值（0-1，默认0.3）
    /// </summary>
    public double? FailureRateWarnThreshold { get; set; } = 0.3;

    /// <summary>
    /// 平均处理耗时告警阈值（毫秒，默认5000）
    /// </summary>
    public double? ProcessTimeWarnThresholdMs { get; set; } = 5000;

    /// <summary>
    /// 是否启用告警通知（默认启用）
    /// </summary>
    public bool EnableAlarm { get; set; } = true;

    /// <summary>
    /// 告警类型（DingTalk/Email/SMS，默认DingTalk）
    /// </summary>
    public string AlarmType { get; set; } = "DingTalk";
}