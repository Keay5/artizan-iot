using Artizan.IoT.TimeSeries.InfluxDB.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Artizan.IoT.TimeSeries.InfluxDB;

[DependsOn(
    typeof(IoTTimeSeriesAbstractionsModule)
)]
public class IoTTimeSeriesInfluxDB2Module : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        // 配置InfluxDB 2.x 选项绑定
        services.Configure<InfluxDbOptions>(configuration.GetSection("IoT:TimeSeries:InfluxDB2"));
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        /* --------------------------------------------------
           模块初始化（应用启动时执行）
           设计思路：初始化 InfluxDB 客户端连接，验证配置有效性
           设计考量：
           1. 提前创建客户端，发现配置错误
           2. 执行健康检查，确保服务可用
           3. 记录模块启动日志，便于排查问题
         */
        await InitializeInfluxDbClientAsync(context.ServiceProvider);
    }

    public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        /* --------------------------------------------------
           模块销毁（应用关闭时执行）
           设计思路：释放 InfluxDB 客户端资源，确保连接正确关闭
           设计考量：
           1. 释放工厂资源，避免内存泄漏
           2. 关闭HTTP连接池，释放网络资源
           3. 记录模块关闭日志，便于排查问题
         */
        DisposeInfluxDbClient(context.ServiceProvider);
    }

    #region 初始化/销毁 InfluxDB客户端
    /// <summary>
    /// 初始化InfluxDB客户端并执行健康检查
    /// 核心职责：创建客户端实例 + 验证服务可用性 + 记录初始化日志
    /// </summary>
    /// <param name="clientFactory">InfluxDB客户端工厂</param>
    /// <param name="logger">日志器</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">工厂/日志器为null时抛出</exception>
    private async Task InitializeInfluxDbClientAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<IoTTimeSeriesInfluxDB2Module>>();
        var influxDbClientFactory = serviceProvider.GetRequiredService<IInfluxDbClientFactory>();
        try
        {
            // 参数校验（防御性编程）
            ArgumentNullException.ThrowIfNull(influxDbClientFactory, nameof(influxDbClientFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            // 1. 创建客户端实例（触发配置验证）
            var client = influxDbClientFactory.GetClient();
            // 2. 执行健康检查
            var isHealthy = await influxDbClientFactory.CheckHealthAsync();

            // 3. 记录差异化日志
            if (isHealthy)
            {
                logger.LogInformation("IoTTimeSeriesInfluxDB2Module initialized successfully. InfluxDB 2.x connection is healthy.");
            }
            else
            {
                logger.LogWarning("IoTTimeSeriesInfluxDB2Module initialized, but InfluxDB 2.x health check failed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize IoTTimeSeriesInfluxDB2Module");
            throw; // 启动失败时终止应用
        }
    }

    /// <summary>
    /// 销毁InfluxDB客户端资源
    /// 核心职责：释放工厂资源 + 关闭连接池 + 记录销毁日志
    /// </summary>
    /// <param name="clientFactory">InfluxDB客户端工厂</param>
    /// <param name="logger">日志器</param>
    private void DisposeInfluxDbClient(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<IoTTimeSeriesInfluxDB2Module>>();
        var influxDbClientFactory = serviceProvider.GetRequiredService<IInfluxDbClientFactory>();

        // 参数校验（防御性编程）
        if (influxDbClientFactory == null)
        {
            logger?.LogWarning("InfluxDB client factory is null, skip dispose operation.");
            return;
        }
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        try
        {
            // 释放工厂资源（关闭HTTP连接池、清理单例等）
            influxDbClientFactory.Dispose();
            logger.LogInformation("IoTTimeSeriesInfluxDB2Module shutdown successfully. InfluxDB 2.x client disposed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while shutting down ArtizanIoTTimeSeriesInfluxDB2Module");
        }
    }
    #endregion
}