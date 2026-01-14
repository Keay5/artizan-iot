using Artizan.IoT.TimeSeries.Exceptions;
using Artizan.IoT.TimeSeries.InfluxDB.Options;
using InfluxDB.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.TimeSeries.InfluxDB;

/// <summary>
/// InfluxDB 2.x 客户端工厂实现
/// 设计思路：
/// 1. 单例模式：标记ISingletonDependency，保证应用生命周期内仅一个实例
/// 2. 懒加载：首次使用时创建客户端，避免启动开销
/// 3. 线程安全：双重检查锁保证多线程下仅创建一次客户端
/// 4. 资源管理：实现IDisposable，应用退出时释放客户端
/// 5  失效重连逻辑:
///   - 连接状态检测：每次获取客户端前检查连接健康状态
///   - 失效重连：连接断开时自动销毁旧客户端并重建
///   - 防抖动：短时间内多次失败时限制重建频率，避免频繁创建
///   - 线程安全：保持双重检查锁，新增重连锁保证原子性
/// 设计模式：工厂模式 + 单例模式 + 懒加载模式
/// 设计考量：
/// - InfluxDBClient是线程安全的，适合单例复用
/// - 连接池配置优化高并发场景的性能
/// - 配置验证提前发现错误，避免运行时异常
/// </summary>
public class InfluxDbClientFactory : IInfluxDbClientFactory, ISingletonDependency
{
    private readonly InfluxDbOptions _options;
    private InfluxDBClient _client;
    private readonly object _lockObj = new object();
    private readonly object _reconnectLockObj = new object(); // 重连专用锁，避免并发重连
    private readonly ILogger<InfluxDbClientFactory> _logger;
    private DateTime _lastReconnectAttempt = DateTime.MinValue; // 最后一次重连尝试时间（防抖动）
    private readonly TimeSpan _minReconnectInterval = TimeSpan.FromSeconds(5); // 最小重连间隔

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">InfluxDB2配置</param>
    /// <param name="logger">日志器</param>
    public InfluxDbClientFactory(
        IOptions<InfluxDbOptions> options,
        ILogger<InfluxDbClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 验证配置
        var validator = new InfluxDbOptionsValidator();
        validator.Validate(_options);

        _logger.LogInformation("InfluxDbClientFactory initialized with Url: {Url}, Org: {Org}, Bucket: {Bucket}",
            _options.Url, _options.Org, _options.Bucket);
    }

    /// <summary>
    /// 线程安全的懒加载创建客户端
    /// 设计思路：双重检查锁（Double-Check Locking）保证线程安全且性能最优
    /// 设计考量：
    /// 1. 先检查客户端是否存在 + 连接是否健康
    /// 2. 连接失效时触发重连逻辑（加锁保证原子性）
    /// 3. 防抖动：短时间内不重复重连，避免资源耗尽
    /// </summary>
    /// <returns>InfluxDBClient单例实例</returns>
    public InfluxDBClient GetClient()
    {
        // 第一步：检查客户端是否存在，且连接健康
        if (_client != null && IsClientHealthy().Result)
        {
            return _client;
        }

        /* 双重检查锁：防止重连锁内并发创建*/
        // 第二步：触发重连逻辑（加锁保证单线程重连）
        lock (_reconnectLockObj)
        {
            // 防抖动：5秒内仅允许一次重连尝试
            var now = DateTime.Now;
            if (now - _lastReconnectAttempt < _minReconnectInterval)
            {
                _logger.LogWarning("Reconnect attempt skipped: minimum reconnect interval ({0}s) not elapsed",
                    _minReconnectInterval.TotalSeconds);
                // 若客户端不为null，仍返回（即使不健康，由上层处理），避免雪崩
                return _client ?? CreateNewClient();
            }

            _lastReconnectAttempt = now;

            // 第三步：销毁旧客户端（若存在）
            if (_client != null)
            {
                try
                {
                    _logger.LogWarning("Destroying unhealthy InfluxDB client for reconnection");
                    _client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispose unhealthy InfluxDB client");
                }
                finally
                {
                    _client = null;
                }
            }

            // 第四步：创建新客户端
            return CreateNewClient();
        }
    }

    /// <summary>
    /// 创建新客户端（抽离为独立方法，便于复用）
    /// 设计思路：双重检查锁保证单例创建，集中处理创建逻辑
    /// </summary>
    /// <returns>新的InfluxDBClient实例</returns>
    private InfluxDBClient CreateNewClient()
    {
        // 第一次检查，无锁，提升性能
        if (_client != null)
        {
            return _client;
        }

        // 加锁保证线程安全
        lock (_lockObj)
        {
            // 第二次检查，防止锁等待期间已创建
            if (_client == null)
            {
                _logger.LogInformation("Creating new InfluxDB 2.x client for Url: {Url}", _options.Url);

                try
                {
                    _client = new InfluxDBClient(new InfluxDBClientOptions(_options.Url)
                    {
                        Token = _options.Token,
                        Org = _options.Org,
                        Bucket = _options.Bucket,
                        Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                        //AllowHttpRedirects = true,
                        //VerifySsl = _options.VerifySsl ?? true // 补充配置项，更灵活
                    });

                    _logger.LogInformation("New InfluxDB 2.x client created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create new InfluxDB 2.x client");
                    throw new TimeSeriesDataException("创建InfluxDB 2.x客户端失败", ex);
                }
            }
        }

        return _client;
    }

    /// <summary>
    /// 检查客户端连接健康状态
    /// 设计思路：通过Ping操作验证连接，提供统一的健康检查接口
    /// 设计考量：
    /// 1. 捕获异常返回false，避免健康检查导致应用崩溃
    /// 2. 记录日志，便于排查连接问题
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否正常</returns>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = GetClient();
            var isHealthy = await client.PingAsync();

            if (isHealthy)
            {
                _logger.LogInformation("InfluxDB 2.x client health check passed");
            }
            else
            {
                _logger.LogWarning("InfluxDB 2.x client health check failed: Ping returned false");
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InfluxDB 2.x client health check failed");
            return false;
        }
    }

    /// <summary>
    /// 检查客户端连接是否健康（轻量级检测）
    /// 设计思路：
    /// 1. 优先用Ping检测（无业务侵入）
    /// 2. 捕获异常视为连接不健康
    /// </summary>
    /// <returns>是否健康</returns>
    private async Task<bool> IsClientHealthy()
    {
        try
        {
            // InfluxDB 2.x 客户端PingAsync无参重载（同步方法）
            return await _client.PingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB client connection is unhealthy");
            return false;
        }
    }

    /// <summary>
    /// 释放客户端资源
    /// 设计思路：单例工厂负责客户端的生命周期管理
    /// 设计考量：
    /// 1. 加锁保证线程安全，防止多线程同时释放
    /// 2. 释放后置空客户端，下次获取时重新创建
    /// 3. 记录日志，便于追踪资源释放
    /// </summary>
    public void Dispose()
    {
        lock (_lockObj)
        {
            if (_client != null)
            {
                _logger.LogInformation("Disposing InfluxDB 2.x client");
                _client.Dispose();
                _client = null;
                _logger.LogInformation("InfluxDB 2.x client disposed successfully");
            }
        }

        GC.SuppressFinalize(this);
    }
}
