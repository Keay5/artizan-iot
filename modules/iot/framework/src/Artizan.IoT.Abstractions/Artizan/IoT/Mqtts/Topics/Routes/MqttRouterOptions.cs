using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Topics.Routes;

/// <summary>
/// 路由系统配置选项（支持ABP设置系统动态配置）
/// 设计理念：生产环境可动态调整核心参数，无需重启应用
/// </summary>
public class MqttRouterOptions
{
    /// <summary>
    /// 最大并发处理数（默认200，根据服务器CPU核心数调整）
    /// 用途：限制同时处理的消息数，避免线程池耗尽
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = 200;

    /// <summary>
    /// 是否启用Topic解析缓存（默认true）
    /// 用途：缓存正则解析结果，提升匹配性能
    /// </summary>
    public bool EnableTopicCache { get; set; } = true;

    /// <summary>
    /// 缓存过期时间（默认30分钟）
    /// 用途：定期清理长期未使用的解析缓存，释放内存
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 缓存清理任务首次执行延迟（默认1分钟）
    /// 读取appsettings.json的Mqtt:Router:CacheCleanupInitialDelay,
    /// json配置示例：
    /// {
    ///  "Mqtt": {
    ///    "Router": {
    ///      "CacheCleanupInterval": "01:00:00", // 清理间隔
    ///    }
    ///  }
    ///}
    /// </summary>
    public TimeSpan CacheCleanupInitialDelay { get; set; } = TimeSpan.FromMinutes(1); // 默认1分钟

    /// <summary>
    /// 缓存清理任务执行间隔（默认1小时）
    /// 读取appsettings.json的Mqtt:Router:CacheCleanupInterval
    /// 
    ///json配置示例：
    ///{
    ///  "Mqtt": {
    ///    "Router": {
    ///      "CacheCleanupInitialDelay": "00:01:00" // 首次延迟
    ///    }
    ///  }
    ///}
    /// </summary>
    public TimeSpan CacheCleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
