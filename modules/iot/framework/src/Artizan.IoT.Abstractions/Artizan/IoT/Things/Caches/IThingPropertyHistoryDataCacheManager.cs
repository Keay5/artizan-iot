using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches;

/// <summary>
/// 设备历史属性数据缓存管理器接口
/// 设计模式：门面模式（Facade Pattern）
/// 设计思路：
/// 1. 封装IoT场景"历史属性数据"的缓存操作，聚焦"时序存储、时间范围查询"核心诉求
/// 2. 与最新数据缓存接口解耦，避免单一接口职责臃肿
/// 设计理念：
/// - 时序数据特性：按时间戳存储多版本属性值，支持时间范围过滤，贴合数据分析/故障回溯场景
/// - 分页查询：历史数据量可能较大，分页避免一次性加载过多数据导致内存溢出
/// 设计考量：
/// - 时间粒度适配：支持分钟级（1/5/10/15分钟）和自定义时间范围查询，覆盖常见IoT分析场景
/// - 数据保留策略：内置数据过期清理逻辑，避免历史数据无限累积
/// </summary>
public interface IThingPropertyHistoryDataCacheManager
{
    /// <summary>
    /// 新增一条历史属性数据（追加模式，不覆盖已有数据）
    /// </summary>
    /// <param name="cacheItem">缓存项（必须包含时间戳）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddAsync(
        ThingPropertyDataCacheItem cacheItem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量新增历史属性数据（适配设备批量上报历史数据场景）
    /// </summary>
    /// <param name="cacheItems">缓存项列表（必须包含时间戳）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddManyAsync(
        IEnumerable<ThingPropertyDataCacheItem> cacheItems,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按时间范围查询历史属性数据（核心能力：时序查询）
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="startTime">开始时间（UTC）</param>
    /// <param name="endTime">结束时间（UTC）</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">页大小（默认100）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果（缓存项列表+总条数）</returns>
    Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByTimeRangeAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        DateTime startTime,
        DateTime endTime,
        int pageIndex = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按快捷时间范围查询历史属性数据（适配1/5/10/15分钟等常见场景）
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="minutes">最近N分钟（支持1/5/10/15）</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetByRecentMinutesAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        int minutes,
        int pageIndex = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定时间范围内的历史属性数据
    /// </summary>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="propertyIdentifier">属性标识符</param>
    /// <param name="startTime">开始时间（UTC）</param>
    /// <param name="endTime">结束时间（UTC）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveByTimeRangeAsync(
        string productKey,
        string deviceName,
        string propertyIdentifier,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的历史数据（按配置的保留时长）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task CleanupExpiredHistoryAsync(CancellationToken cancellationToken = default);
}