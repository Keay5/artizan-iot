using Artizan.IoT.Things.Caches.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.Options;

/// <summary>
/// 最新属性数据缓存专属配置
/// 设计思路：
/// 1. 继承通用配置基类，补充最新数据的专属配置项
/// 2. 与历史数据配置解耦，避免配置项冗余
/// 设计考量：
/// - 默认过期时间：30分钟，适配IoT设备属性的更新频率（既保证实时性，又减少缓存重建开销）
/// - 批量写入阈值：100条，平衡批量写入的吞吐量与单次操作的耗时
/// </summary>
public class ThingPropertyDataCacheOptions : ThingCacheOptions
{
    /// <summary>
    /// 默认缓存过期时间（全局默认30分钟）
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 默认过期模式（默认绝对过期，避免高频访问的属性永久缓存）
    /// </summary>
    public CacheExpireMode DefaultExpireMode { get; set; } = CacheExpireMode.Absolute;

    /// <summary>
    /// 批量写入阈值（超过该数量则异步批量写入，默认100）
    /// </summary>
    public int BatchWriteThreshold { get; set; } = 100;
}
