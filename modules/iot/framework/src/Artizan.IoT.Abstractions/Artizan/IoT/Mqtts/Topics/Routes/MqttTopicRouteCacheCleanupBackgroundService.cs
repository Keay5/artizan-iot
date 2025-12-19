using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Topics.Routes;

/// <summary>
/// 定时清理Topic解析缓存的后台服务（生产环境避免内存泄漏）
/// 设计理念：
/// 1. 定期释放过期缓存：平衡正则解析性能（缓存）与内存占用（过期清理）；
/// 2. 适配Host生命周期：通过stoppingToken感知应用停止，优雅终止Timer，避免内存泄漏；
/// 3. 异常隔离：清理缓存的异常不影响服务本身，仅记录日志；
/// 4. 线程安全：Timer回调通过DI创建独立作用域，避免多线程DI上下文冲突。
/// </summary>
public class MqttTopicRouteCacheCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MqttTopicRouteCacheCleanupBackgroundService> _logger;
    private readonly MqttRouterOptions _routerOptions; // 注入配置选项
    private Timer? _timer; // 改为可空类型，避免未初始化警告
    private readonly SemaphoreSlim _executionLock = new(1, 1); // 防止并发执行清理（如清理耗时超过1小时）

    public MqttTopicRouteCacheCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MqttTopicRouteCacheCleanupBackgroundService> logger,
            IOptions<MqttRouterOptions> routerOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _routerOptions = routerOptions.Value; // 读取配置值（自动绑定appsettings.json）
    }

    /// <summary>
    /// 后台服务核心执行方法（Host启动时调用）
    /// 修复点：
    /// 1. 移除值类型参数传递，通过闭包捕获stoppingToken；
    /// 2. Timer回调不依赖state参数，避免as运算符转换值类型的错误；
    /// 3. 增加Timer创建的空值校验，符合.NET 8.0+ nullable规范。
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
             "「MQTT Topic缓存」清理后台服务已启动 | 首次执行延迟：{InitialDelay} | 执行间隔：{Interval}",
             _routerOptions.CacheCleanupInitialDelay,
             _routerOptions.CacheCleanupInterval);

        // 每小时执行一次缓存清理（首次延迟1分钟执行，避免应用启动时竞争资源）
        _timer = new Timer(
            async _ => await ExecuteCacheCleanupAsync(stoppingToken), // 闭包捕获stoppingToken，无需state参数
            null, // 无state参数，避免值类型传递问题
            _routerOptions.CacheCleanupInitialDelay, // 首次执行延迟
            _routerOptions.CacheCleanupInterval);    // 后续执行间隔

        // 注册应用停止时的回调：终止Timer并等待当前清理完成
        stoppingToken.Register(() =>
        {
            _logger.LogInformation("应用停止，终止「MQTT Topic缓存」清理Timer");
            _timer?.Change(Timeout.Infinite, 0); // 停止Timer触发新的回调
            _executionLock.Wait(); // 等待当前清理任务完成
            _executionLock.Dispose();
            _timer?.Dispose();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// 实际执行缓存清理的方法（封装核心逻辑，便于复用和测试）
    /// </summary>
    private async Task ExecuteCacheCleanupAsync(CancellationToken stoppingToken)
    {
        // 防止并发执行：同一时间仅允许一个清理任务运行
        if (!_executionLock.Wait(0, stoppingToken))
        {
            _logger.LogDebug("缓存清理任务正在执行中，跳过本次触发");
            return;
        }

        try
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("应用正在停止，跳过缓存清理");
                return;
            }

            _logger.LogInformation("开始执行Topic解析缓存清理");

            // 创建独立的DI作用域：避免多线程共享同一DI上下文，防止线程安全问题
            using var scope = _serviceProvider.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<MqttTopicTemplateParser>();

            // 执行缓存清理（核心逻辑）
            parser.ClearExpiredCache();

            _logger.LogInformation("Topic解析缓存清理完成");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("缓存清理任务被取消（应用正在停止）");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Topic解析缓存清理时发生异常");
        }
        finally
        {
            // 释放执行锁，允许下一次清理执行
            _executionLock.Release();
        }
    }

    /// <summary>
    /// 释放资源（遵循BackgroundService规范）
    /// </summary>
    public override void Dispose()
    {
        _timer?.Dispose();
        _executionLock?.Dispose();
        base.Dispose();
    }
}


#region 旧的设计
///// <summary>
///// 定时清理Topic解析缓存的后台服务（生产环境避免内存泄漏）
///// 设计理念：定期释放过期缓存，平衡性能与内存占用
///// </summary>
//public class TopicCacheCleanupService : BackgroundService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<TopicCacheCleanupService> _logger;
//    private Timer _timer;

//    public TopicCacheCleanupService(IServiceProvider serviceProvider, ILogger<TopicCacheCleanupService> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;
//    }

//    protected override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        // 每小时执行一次缓存清理
//        _timer = new Timer(
//            async (state) => await CleanupCacheAsync(state as CancellationToken),
//            stoppingToken,
//            TimeSpan.Zero,
//            TimeSpan.FromHours(1));

//        return Task.CompletedTask;
//    }

//    private async Task CleanupCacheAsync(CancellationToken stoppingToken)
//    {
//        if (stoppingToken.IsCancellationRequested)
//            return;

//        using var scope = _serviceProvider.CreateScope();
//        var parser = scope.ServiceProvider.GetRequiredService<TopicTemplateParser>();

//        try
//        {
//            _logger.LogInformation("开始清理过期的Topic解析缓存");
//            parser.ClearExpiredCache();
//            _logger.LogInformation("Topic解析缓存清理完成");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "清理Topic解析缓存时发生异常");
//        }

//        await Task.CompletedTask;
//    }

//    public override void Dispose()
//    {
//        _timer?.Dispose();
//        base.Dispose();
//    }
//} 
#endregion