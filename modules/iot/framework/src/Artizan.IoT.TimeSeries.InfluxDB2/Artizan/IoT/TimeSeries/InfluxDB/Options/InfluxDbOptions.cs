namespace Artizan.IoT.TimeSeries.InfluxDB.Options;

/// <summary>
/// InfluxDB 2.x 配置选项
/// 设计思路：继承基础配置，扩展InfluxDB2专属配置
/// 设计模式：选项模式（Options Pattern），适配ABP配置系统
/// 设计考量：
/// 1. 区分通用配置和版本专属配置，便于扩展
/// 2. 内置默认值，降低配置复杂度
/// 3. 包含连接池和批量写入配置，优化性能
/// </summary>
public class InfluxDbOptions
{
    /// <summary>
    /// InfluxDB服务地址（如 http://localhost:8086）
    /// </summary>
    public string Url { get; set; } = "http://localhost:8086";

    /// <summary>
    /// 认证Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 组织ID
    /// </summary>
    public string Org { get; set; } = string.Empty;

    /// <summary>
    /// 桶名称（对应V1的Database）
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 查询超时时间（秒）
    /// </summary>
    public int QueryTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 批量写入批次大小
    /// </summary>
    public int BatchSize { get; set; } = TimeSeriesConsts.DefaultBatchThreshold;

    /// <summary>
    /// 批量写入刷新间隔（毫秒）
    /// </summary>
    public int FlushIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    public int RetryIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 每个服务器最大连接数（连接池配置）
    /// </summary>
    public int MaxConnectionsPerServer { get; set; } = 100;
}
