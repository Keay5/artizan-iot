using Artizan.IoT.TimeSeries.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据读取接口
/// 设计思路：分离读写接口，遵循接口隔离原则
/// 设计模式：接口隔离模式（ISP）
/// 设计考量：
/// 1. 只读场景只需依赖此接口，降低耦合
/// 2. 统一查询和聚合方法签名，便于替换实现
/// </summary>
public interface ITimeSeriesDataReader : ITimeSeriesStorageContract
{
    /// <summary>
    /// 按条件查询原始数据
    /// </summary>
    /// <param name="criteria">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>时序数据列表</returns>
    Task<IReadOnlyList<TimeSeriesData>> QueryAsync(
        TimeSeriesQueryCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按条件聚合查询
    /// </summary>
    /// <param name="criteria">聚合查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>聚合结果（键：窗口起始时间，值：聚合值）</returns>
    Task<IReadOnlyDictionary<DateTime, double>> AggregateAsync(
        TimeSeriesAggregateCriteria criteria,
        CancellationToken cancellationToken = default);
}
