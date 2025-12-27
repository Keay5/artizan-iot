using Artizan.IoT.BatchProcessing.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Configurations;

/// <summary>
/// 批处理核心配置项
/// 【设计思路】：配置驱动设计，所有核心参数可外部配置，无需修改代码
/// 【设计考量】：
/// 1. 所有配置项提供默认值，降低使用门槛
/// 2. 分区相关配置支持动态调整，适配流量变化
/// 3. 策略相关配置（熔断/重试/隔离）独立配置，便于精细化管控
/// 【设计模式】：POCO（Plain Old CLR Object）+ 配置绑定
/// </summary>
public class BatchProcessingOptions
{
    /// <summary>
    /// 初始分区数量（默认8）
    /// </summary>
    public int PartitionCount { get; set; } = 8;

    /// <summary>
    /// 是否启用动态分区扩容/缩容（默认启用）
    /// </summary>
    public bool EnableDynamicPartition { get; set; } = true;

    /// <summary>
    /// 最大分区数（扩容上限，默认32）
    /// </summary>
    public int MaxPartitionCount { get; set; } = 32;

    /// <summary>
    /// 最小分区数（缩容下限，默认4）
    /// </summary>
    public int MinPartitionCount { get; set; } = 4;

    /// <summary>
    /// 分区扩容阈值（平均队列长度，默认1000）
    /// </summary>
    public int PartitionExpandThreshold { get; set; } = 1000;

    /// <summary>
    /// 分区缩容阈值（平均队列长度，默认100）
    /// </summary>
    public int PartitionShrinkThreshold { get; set; } = 100;

    /// <summary>
    /// 分区调整间隔（避免频繁调整，默认5分钟）
    /// </summary>
    public TimeSpan PartitionAdjustInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 是否启用分区调整告警（默认启用）
    /// </summary>
    public bool EnablePartitionAlarm { get; set; } = true;

    /// <summary>
    /// 每个分区最大并发数（隔离策略，默认4）
    /// </summary>
    public int IsolateMaxConcurrencyPerPartition { get; set; } = 4;

    /// <summary>
    /// 熔断失败阈值（默认10次）
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 10;

    /// <summary>
    /// 熔断恢复时间（默认1分钟）
    /// </summary>
    public TimeSpan CircuitBreakerRecoveryTime { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 最大重试次数（默认3次）
    /// </summary>
    public int RetryMaxCount { get; set; } = 3;

    /// <summary>
    /// 重试间隔（默认1秒）
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 默认执行模式（默认并行）
    /// </summary>
    public ExecutionMode DefaultExecutionMode { get; set; } = ExecutionMode.Parallel;

    /// <summary>
    /// 分区执行模式覆盖配置（针对特定分区的自定义模式）
    /// </summary>
    public Dictionary<string, ExecutionMode> PartitionExecutionModes { get; set; } = new Dictionary<string, ExecutionMode>();

    /// <summary>
    /// 执行模式切换超时时间（默认30秒）
    /// </summary>
    public TimeSpan ExecutionModeSwitchTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 兜底存储类型（File/Redis，默认File）
    /// </summary>
    public string FallbackStoreType { get; set; } = "File";

    /// <summary>
    /// 文件兜底存储路径（默认./FallbackStore）
    /// </summary>
    public string FallbackFileStorePath { get; set; } = "./FallbackStore";

    /// <summary>
    /// Redis连接字符串（兜底存储/幂等性，默认localhost:6379）
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 幂等记录过期时间（默认24小时）
    /// </summary>
    public TimeSpan IdempotentRecordExpiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// 批处理大小（默认100条/批）
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 批处理间隔（默认500ms）
    /// </summary>
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}