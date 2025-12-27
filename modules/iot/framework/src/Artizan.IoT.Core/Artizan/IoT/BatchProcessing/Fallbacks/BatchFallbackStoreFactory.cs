using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Configurations;
using Artizan.IoT.BatchProcessing.Fallbacks.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Fallbacks;

/// <summary>
/// 兜底存储工厂
/// 【设计思路】：工厂模式，根据配置创建不同类型的兜底存储
/// 【设计考量】：
/// 1. 支持File/Redis两种存储类型
/// 2. 单例创建，避免重复初始化
/// 3. 类型校验，确保创建的存储类型正确
/// 【设计模式】：工厂模式（Factory Pattern）
/// </summary>
public class BatchFallbackStoreFactory
{
    /// <summary>
    /// 批处理配置
    /// </summary>
    private readonly BatchProcessingOptions _options;

    /// <summary>
    /// 日志器工厂
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Redis连接多路复用器（懒加载）
    /// </summary>
    private readonly Lazy<ConnectionMultiplexer> _redisMultiplexer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">批处理配置</param>
    /// <param name="loggerFactory">日志器工厂</param>
    public BatchFallbackStoreFactory(
        IOptions<BatchProcessingOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        // 懒加载创建Redis连接
        _redisMultiplexer = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(_options.RedisConnectionString);
        });
    }

    /// <summary>
    /// 创建兜底存储
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <returns>兜底存储实例</returns>
    public IBatchFallbackStore<TMessage> CreateStore<TMessage>()
    {
        return CreateStore<TMessage>(_options.FallbackStoreType);
    }

    /// <summary>
    /// 创建指定类型的兜底存储
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="storeType">存储类型（File/Redis）</param>
    /// <returns>兜底存储实例</returns>
    public IBatchFallbackStore<TMessage> CreateStore<TMessage>(string storeType)
    {
        if (string.IsNullOrEmpty(storeType))
        {
            storeType = "File";
        }

        switch (storeType.ToLowerInvariant())
        {
            case "redis":
                return new RedisBatchFallbackStore<TMessage>(
                    _redisMultiplexer.Value,
                    _loggerFactory.CreateLogger<RedisBatchFallbackStore<TMessage>>());

            case "file":
            default:
                return new FileBatchFallbackStore<TMessage>(
                    _options.FallbackFileStorePath,
                    _loggerFactory.CreateLogger<FileBatchFallbackStore<TMessage>>());
        }
    }
}
