using Artizan.IoT.Things.Caches.BackgroundServices;
using Artizan.IoT.Things.Caches.Enums;
using Artizan.IoT.Things.Caches.Managers;
using Artizan.IoT.Things.Caches.Options;
using Artizan.IoT.Things.Caches.StorageProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;

namespace Artizan.IoT.Things.Caches.Extensions;

/// <summary>
/// 设备缓存服务依赖注入扩展方法
/// 设计思路：
/// 1. 封装缓存服务的注册逻辑，简化上层调用（一行代码完成所有注册）
/// 2. 基于配置自动选择存储提供者（本地内存/Redis），适配不同环境
/// 设计理念：
/// - 约定优于配置：提供默认配置，无需手动注册每个服务
/// - 可扩展：预留自定义存储提供者的扩展点
/// 设计考量：
/// - 生命周期管理：
///   - 存储提供者：Singleton（减少连接/内存开销）
///   - 管理器：Scoped（适配业务上下文）
///   - 后台服务：Singleton（单例后台任务）
/// - 配置绑定：支持从配置文件（appsettings.json）绑定缓存选项
/// </summary>
public static class ThingCacheServiceCollectionExtensions
{
    /// <summary>
    /// 添加设备缓存服务（核心扩展方法）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureLatestOptions">最新数据缓存配置</param>
    /// <param name="configureHistoryOptions">历史数据缓存配置</param>
    /// <returns>服务集合（链式调用）</returns>
    /// <exception cref="ArgumentNullException">服务集合为空时抛出</exception>
    public static IServiceCollection AddThingCache(
        this IServiceCollection services,
        Action<ThingPropertyDataCacheOptions>? configureLatestOptions = null,
        Action<ThingPropertyHistoryDataCacheOptions>? configureHistoryOptions = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // 1. 注册配置项（支持从配置文件绑定 + 代码配置）
        if (configureLatestOptions != null)
        {
            services.Configure(configureLatestOptions);
        }
        else
        {
            // 默认配置
            services.Configure<ThingPropertyDataCacheOptions>(options => { });
        }

        if (configureHistoryOptions != null)
        {
            services.Configure(configureHistoryOptions);
        }
        else
        {
            // 默认配置
            services.Configure<ThingPropertyHistoryDataCacheOptions>(options => { });
        }

        // 2. 根据配置自动选择，注册存储提供者（注册为单例）
        services.AddSingleton<IThingCacheStorageProvider>(sp =>
        {
            // 获取最新数据缓存配置（通用配置基类）
            var latestOptions = sp.GetRequiredService<IOptions<ThingPropertyDataCacheOptions>>().Value;

            return latestOptions.StorageType switch
            {
                CacheStorageType.Redis =>
                    // Redis存储：创建ConnectionMultiplexer + 注册ThingRedisCacheProvider
                    CreateRedisStorageProvider(sp, latestOptions),

                CacheStorageType.LocalMemory =>
                    // 本地内存存储：注册ThingLocalMemoryCacheProvider
                    new ThingLocalMemoryCacheProvider(),
                _ => throw new NotSupportedException($"不支持的缓存存储类型：{latestOptions.StorageType}")
            };
        });

        // 3. 注册缓存管理器（Scoped生命周期）
        services.TryAddScoped<IThingPropertyDataCacheManager, ThingPropertyDataCacheManager>();
        services.TryAddScoped<IThingPropertyHistoryDataCacheManager, ThingPropertyHistoryDataCacheManager>();

        // 4. 注册缓存清理后台服务（Singleton）
        services.AddHostedService<ThingCacheCleanupBackgroundService>();

        return services;
    }

    #region 私有辅助方法
    /// <summary>
    /// 创建Redis存储提供者（封装Redis连接创建逻辑）
    /// </summary>
    /// <param name="serviceProvider">服务提供器（需在服务配置阶段传入，能获取到IServiceCollection）</param>
    /// <param name="options">Redis配置项</param>
    /// <returns>Redis存储提供者实例</returns>
    /// <exception cref="InvalidOperationException">Redis连接字符串未配置时抛出</exception>
    private static ThingRedisCacheProvider CreateRedisStorageProvider(
        IServiceProvider serviceProvider,
        ThingPropertyDataCacheOptions options)
    {
        // 1. 校验Redis连接字符串
        if (string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            throw new InvalidOperationException("Redis连接字符串未配置，请在ThingPropertyDataCacheOptions中设置RedisConnectionString");
        }

        // 2. 尝试从容器中获取已注册的IConnectionMultiplexer实例
        var existingMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
        IConnectionMultiplexer redisConnection;

        if (existingMultiplexer != null)
        {
            // 复用已有连接
            redisConnection = existingMultiplexer;
        }
        else
        {
            // 3. 无已有连接则创建新实例，并注册为Singleton
            redisConnection = ConnectionMultiplexer.Connect(options.RedisConnectionString);

            // 获取服务集合（仅在ConfigureServices阶段有效，若在运行时调用需调整逻辑）
            var serviceCollection = serviceProvider.GetRequiredService<IServiceCollection>();
            // 特别注意：注册为Singleton，避免重复创建。 ConnectionMultiplexer 是线程安全且应全局单例的
            serviceCollection.AddSingleton<IConnectionMultiplexer>(redisConnection);
        }

        // 4. 创建并返回Redis存储提供者
        return new ThingRedisCacheProvider((ConnectionMultiplexer)redisConnection, options.RedisDatabase);
    }
    #endregion
}