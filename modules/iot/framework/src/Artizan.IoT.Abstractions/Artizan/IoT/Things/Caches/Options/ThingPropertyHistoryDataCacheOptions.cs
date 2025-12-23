using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.Options;

/// <summary>
/// 历史属性数据缓存专属配置
/// 设计思路：
/// 1. 继承通用配置基类，补充历史数据的时序特性配置
/// 2. 聚焦"数据保留、时间分片"核心诉求
/// 设计考量：
/// - 历史数据保留时长：默认7天，避免无限累积导致存储溢出
/// - 时间分片大小：5分钟，适配常见的IoT分钟级数据分析场景
/// - 默认分页大小：100条，平衡查询性能与内存占用
/// </summary>
public class ThingPropertyHistoryDataCacheOptions : ThingCacheOptions
{
    /// <summary>
    /// 历史数据保留时长（默认7天，超过则自动清理）
    /// </summary>
    public TimeSpan HistoryRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 时间分片大小（默认5分钟，用于数据分片存储，提升查询效率）
    /// </summary>
    public int TimeSliceMinutes { get; set; } = 5;

    /// <summary>
    /// 默认分页大小（默认100条）
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>
    /// 最大分页大小（防止单次查询过多数据，默认1000）
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;
}