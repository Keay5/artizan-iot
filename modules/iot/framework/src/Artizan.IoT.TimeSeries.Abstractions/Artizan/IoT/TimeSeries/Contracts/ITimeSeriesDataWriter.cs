using Artizan.IoT.TimeSeries.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据写入接口
/// 设计思路：分离读写接口，支持只写场景的极简实现
/// 设计模式：接口隔离模式（ISP）
/// 设计考量：
/// 1. IoT场景写多读少，单独封装写入接口提升性能
/// 2. 区分单条和批量写入，批量写入优化性能
/// 3. 删除方法增加警告，引导归档而非删除
/// </summary>
public interface ITimeSeriesDataWriter : ITimeSeriesStorageContract
{
    /// <summary>
    /// 单条写入（支持幂等）
    /// </summary>
    /// <param name="data">时序数据</param>
    /// <param name="options">写入选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>写入结果</returns>
    Task<TimeSeriesWriteResult> WriteAsync(
        TimeSeriesData data,
        TimeSeriesWriteOptions options = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量写入（高并发场景首选）
    /// </summary>
    /// <param name="dataList">时序数据列表</param>
    /// <param name="options">写入选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量写入结果</returns>
    Task<TimeSeriesBatchWriteResult> BatchWriteAsync(
        IEnumerable<TimeSeriesData> dataList,
        TimeSeriesWriteOptions options = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按条件删除数据（慎用，时序数据建议归档而非删除）
    /// </summary>
    /// <param name="criteria">删除条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    Task<TimeSeriesDeleteResult> DeleteAsync(
        TimeSeriesDeleteCriteria criteria,
        CancellationToken cancellationToken = default);
}
