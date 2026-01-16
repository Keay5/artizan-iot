using Volo.Abp;

namespace Artizan.IoT.TimeSeries.InfluxDB.Options;

/// <summary>
/// InfluxDB2配置验证器
/// 设计思路：提前验证配置有效性，避免运行时异常
/// 设计模式：验证器模式（Validator Pattern）
/// 设计考量：
/// 1. 启动时验证核心配置，快速发现配置错误
/// 2. 符合ABP配置验证规范，集成到配置系统
/// </summary>
public class InfluxDbOptionsValidator
{
    public void Validate(InfluxDbOptions options)
    {
        Check.NotNullOrWhiteSpace(options.Url, nameof(options.Url));
        Check.NotNullOrWhiteSpace(options.Token, nameof(options.Token));
        Check.NotNullOrWhiteSpace(options.Org, nameof(options.Org));
        Check.NotNullOrWhiteSpace(options.Bucket, nameof(options.Bucket));
    }
}
