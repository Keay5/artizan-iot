using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Artizan.IoT.Mqtt.Options;

public static class MqttRouterOptionsExtensions
{
    /// <summary>
    /// 注册Mqtt路由配置选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigMqttRouterOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // 第一步：绑定配置文件中的Mqtt:Router节点到配置类
        services.Configure<MqttRouterOptions>(configuration.GetSection("IoT:Mqtt:Router"));

        // 第二步：确保默认值生效（关键）
        // PostConfigure会在所有Configure执行后执行，保证默认值不会被null覆盖
        services.PostConfigure<MqttRouterOptions>(options =>
        {
            // 如果配置文件中没有设置，使用默认值（这里是兜底，因为属性已经有默认值）
            // 可以在这里添加额外的默认值逻辑，比如根据CPU核心数动态调整
            options.CacheExpiration = options.CacheExpiration == default ? TimeSpan.FromMinutes(30) : options.CacheExpiration;
            options.CacheCleanupInitialDelay = options.CacheCleanupInitialDelay == default ? TimeSpan.FromMinutes(1) : options.CacheCleanupInitialDelay;
            options.CacheCleanupInterval = options.CacheCleanupInterval == default ? TimeSpan.FromHours(1) : options.CacheCleanupInterval;
        });

        return services;
    }
}
