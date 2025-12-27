using Artizan.IoT.Things.Caches.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.Options;

/// <summary>
/// 设备缓存通用配置基类
/// 设计思路：
/// 1. 提取所有缓存类型的通用配置，避免重复定义
/// 2. 作为派生类的基类，兼顾配置复用与隔离
/// 设计理念：
/// - 最小配置原则：提供合理默认值，仅需配置必要参数即可运行
/// - 多存储适配：Redis配置仅在存储类型为Redis时生效，避免无效配置
/// </summary>
public class ThingCacheOptions
{
    /// <summary>
    /// 缓存存储类型（默认本地内存，生产环境可切换为Redis）
    /// </summary>
    public CacheStorageType StorageType { get; set; } = CacheStorageType.LocalMemory;

    /// <summary>
    /// Redis连接字符串（存储类型为Redis时必填）
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis数据库索引（默认0，支持多业务隔离）
    /// </summary>
    public int RedisDatabase { get; set; } = 0;

    /// <summary>
    /// 缓存清理间隔（仅本地存储生效，默认1小时清理一次过期缓存）
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
