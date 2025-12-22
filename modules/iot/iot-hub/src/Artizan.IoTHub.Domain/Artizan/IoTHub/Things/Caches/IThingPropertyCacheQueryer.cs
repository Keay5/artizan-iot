using Artizan.IoTHub.Things.Caches;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoTub.Things.Caches;

/// <summary>
/// 设备属性缓存查询服务接口（定义查询契约）
/// 设计理念：面向接口编程，隔离查询逻辑与业务逻辑，便于扩展和单元测试
/// 核心职责：提供单设备/多设备的最新属性、历史属性查询能力
/// </summary>
public interface IThingPropertyCacheQueryer
{
    /// <summary>
    /// 单设备查询最新属性值
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新属性缓存项（null表示无数据）</returns>
    Task<ThingPropertyDataCacheItem?> GetLatestPropertyAsync(string productKey, string deviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 单设备查询指定时长内的历史属性数据
    /// </summary>
    /// <param name="productKey">产品密钥</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="retainMinutes">保留分钟数（查询近N分钟数据）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>过滤后的历史属性数据列表</returns>
    Task<List<ThingPropertyDataCacheItem>> GetHistoryPropertyAsync(string productKey, string deviceName, int retainMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量查询多设备最新属性值（ABP批量API）
    /// 设计考量：针对多设备查询场景，减少Redis交互次数，提升性能
    /// </summary>
    /// <param name="deviceList">设备列表（产品密钥+设备名称）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键：缓存键，值：最新属性缓存项（null表示无数据）</returns>
    Task<Dictionary<string, ThingPropertyDataCacheItem?>> GetLatestPropertiesBatchAsync(List<(string ProductKey, string DeviceName)> deviceList, CancellationToken cancellationToken = default);
}
