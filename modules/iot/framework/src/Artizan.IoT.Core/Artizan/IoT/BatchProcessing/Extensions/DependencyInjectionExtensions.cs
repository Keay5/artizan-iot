using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Configurations;
using Artizan.IoT.BatchProcessing.Core;
using Artizan.IoT.BatchProcessing.Fallbacks;
using Artizan.IoT.BatchProcessing.Health;
using Artizan.IoT.BatchProcessing.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Extensions;

/// <summary>
/// 依赖注入扩展方法
/// 【设计思路】：提供简洁的扩展方法，一键注册所有批处理相关服务
/// 【设计考量】：
/// 1. 支持配置绑定，灵活配置批处理参数
/// 2. 服务生命周期合理：单例/作用域/瞬时
/// 3. 支持自定义替换默认策略
/// 【设计模式】：扩展方法模式
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 添加批处理核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBatchProcessingCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // 1. 注册配置
        services.Configure<BatchProcessingOptions>(configuration.GetSection("IoT:BatchProcessing"));

        // 2. 注册核心服务
        services.TryAddSingleton<PartitionHealthChecker>();
        services.TryAddSingleton<PartitionDispatcher>();
        services.TryAddSingleton<BatchFallbackStoreFactory>();

        // 3. 注册策略（默认实现）
        services.TryAddSingleton<IIsolateStrategy, SemaphoreIsolateStrategy>();
        services.TryAddSingleton<ICircuitBreakerStrategy, SimpleCircuitBreakerStrategy>();
        services.TryAddSingleton<IRetryStrategy, FixedIntervalRetryStrategy>();
        services.TryAddSingleton<IDegradeStrategy, BasicDegradeStrategy>();
        services.TryAddSingleton<IExecutionOrderStrategy, ExecutionOrderStrategy>();
        services.TryAddSingleton<IUpdatablePartitionStrategy, HashPartitionStrategy>();

        // 4. 注册批处理器基类（瞬时，因为包含状态）
        //services.TryAddTransient<BatchProcessorBase>();

        _ = services.AddOptions<BatchProcessingOptions>()
            //.ValidateDataAnnotations()
            .Validate(options =>
            {
                // 配置校验
                if (options.PartitionCount < 1)
                {
                    throw new ArgumentException("分区数必须大于0");
                }

                if (options.BatchSize < 1)
                {
                    throw new ArgumentException("批处理大小必须大于0");
                }

                if (options.BatchInterval <= TimeSpan.Zero)
                {
                    throw new ArgumentException("批处理间隔必须大于0");
                }

                if (options.MinPartitionCount > options.MaxPartitionCount)
                {
                    throw new ArgumentException("最小分区数不能大于最大分区数");
                }

                return true;
            });

        return services;
    }

    /// <summary>
    /// 添加自定义批处理器
    /// </summary>
    /// <typeparam name="TProcessor">批处理器类型</typeparam>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="lifetime">服务生命周期</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBatchProcessor<TProcessor, TMessage>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TProcessor : class, IBatchProcessor<TMessage>
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // 根据生命周期注册批处理器
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton<IBatchProcessor<TMessage>, TProcessor>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IBatchProcessor<TMessage>, TProcessor>();
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IBatchProcessor<TMessage>, TProcessor>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        return services;
    }

    /// <summary>
    /// 替换默认的分区策略
    /// </summary>
    /// <typeparam name="TStrategy">自定义分区策略类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ReplacePartitionStrategy<TStrategy>(
        this IServiceCollection services)
        where TStrategy : class, IUpdatablePartitionStrategy
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.RemoveAll<IUpdatablePartitionStrategy>();
        services.AddSingleton<IUpdatablePartitionStrategy, TStrategy>();
        return services;
    }

    /// <summary>
    /// 替换默认的熔断策略
    /// </summary>
    /// <typeparam name="TStrategy">自定义熔断策略类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ReplaceCircuitBreakerStrategy<TStrategy>(
        this IServiceCollection services)
        where TStrategy : class, ICircuitBreakerStrategy
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.RemoveAll<ICircuitBreakerStrategy>();
        services.AddSingleton<ICircuitBreakerStrategy, TStrategy>();
        return services;
    }
}
