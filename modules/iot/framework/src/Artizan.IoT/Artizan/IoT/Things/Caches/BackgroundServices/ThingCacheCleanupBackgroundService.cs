using Artizan.IoT.Things.Caches.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.BackgroundServices;

/// <summary>
/// 设备缓存清理后台服务（定时任务）
/// 设计模式：后台服务模式（IHostedService + BackgroundService）
/// 设计思路：
/// 1. 基于.NET内置BackgroundService实现定时清理过期缓存，避免内存泄漏/Redis数据冗余
/// 2. 适配不同存储类型：本地内存需手动清理，Redis依赖内置机制
/// 设计理念：
/// - 非侵入式：后台异步执行，不阻塞主线程业务逻辑
/// - 可配置化：清理间隔通过配置项控制，适配不同业务场景
/// 设计考量：
/// - 异常容错：单个清理周期失败不影响后续执行，记录日志保证可追溯
/// - 优雅关闭：响应应用停止信号，完成当前清理周期后退出
/// - 多缓存类型支持：同时清理最新数据和历史数据的过期项
/// </summary>
public class ThingCacheCleanupBackgroundService : BackgroundService
{
    private readonly IThingCacheStorageProvider _storageProvider;
    private readonly IThingPropertyHistoryDataCacheManager _historyCacheManager;
    private readonly ThingCacheOptions _cacheOptions;
    private readonly TimeSpan _cleanupInterval;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // 避免并发清理

    /// <summary>
    /// 构造函数（依赖注入）
    /// </summary>
    public ThingCacheCleanupBackgroundService(
        IThingCacheStorageProvider storageProvider,
        IThingPropertyHistoryDataCacheManager historyCacheManager,
        IOptions<ThingCacheOptions> cacheOptions)
    {
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _historyCacheManager = historyCacheManager ?? throw new ArgumentNullException(nameof(historyCacheManager));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        // 清理间隔：配置项 > 默认1小时
        _cleanupInterval = _cacheOptions.CleanupInterval > TimeSpan.Zero
            ? _cacheOptions.CleanupInterval
            : TimeSpan.FromHours(1);
    }

    /// <summary>
    /// 后台任务核心逻辑（定时执行清理）
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 应用启动后先执行一次清理
        await CleanupAsync(stoppingToken);

        // 定时循环执行（直到应用停止）
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 等待清理间隔（响应停止信号）
                await Task.Delay(_cleanupInterval, stoppingToken);
                // 执行清理
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 应用停止时抛出，正常退出
                break;
            }
            catch (Exception ex)
            {
                // 记录日志（建议接入日志框架）
                // Log.Error(ex, "设备缓存清理任务执行失败");
                // 失败后等待1分钟重试，避免高频报错
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 执行缓存清理（核心逻辑）
    /// </summary>
    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        // 加锁避免并发清理
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            return; // 已有清理任务在执行，跳过
        }

        try
        {
            // 1. 清理存储层过期缓存（本地内存/Redis）
            await _storageProvider.CleanupExpiredAsync(cancellationToken);
            // 2. 清理历史数据中超过保留时长的项
            await _historyCacheManager.CleanupExpiredHistoryAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release(); // 释放锁
        }
    }

    /// <summary>
    /// 资源释放
    /// </summary>
    public override void Dispose()
    {
        _semaphore.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}