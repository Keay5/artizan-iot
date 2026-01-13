using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据存储基础契约
/// 设计思路：定义所有存储实现必须遵守的核心契约
/// 设计模式：接口隔离原则（ISP），最小接口设计
/// 设计考量：
/// 1. 所有实现必须支持健康检查，便于监控
/// 2. 统一标识存储引擎类型，便于日志和监控
/// 3. 实现IDisposable，保证资源释放
/// </summary>
public interface ITimeSeriesStorageContract : IDisposable
{
    /// <summary>
    /// 存储引擎名称（如 InfluxDB v2、InfluxDB v3）
    /// </summary>
    string StorageEngine { get; }

    /// <summary>
    /// 检查存储连接状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否正常</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
