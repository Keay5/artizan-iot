using Artizan.IoT.TimeSeries.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据事务对象
/// 设计思路：封装事务操作，支持提交/回滚
/// 设计模式：事务模式（Transaction Pattern）
/// 设计考量：
/// 1. 事务ID用于追踪和日志
/// 2. 支持异步释放，避免资源泄露
/// 3. 仅包含事务内的核心操作
/// </summary>
public interface ITimeSeriesTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 事务ID
    /// </summary>
    string TransactionId { get; }

    /// <summary>
    /// 提交事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提交结果</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回滚结果</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 事务内单条写入
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
    /// 事务内批量写入
    /// </summary>
    /// <param name="dataList">时序数据列表</param>
    /// <param name="options">写入选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量写入结果</returns>
    Task<TimeSeriesBatchWriteResult> BatchWriteAsync(
        IEnumerable<TimeSeriesData> dataList,
        TimeSeriesWriteOptions options = default,
        CancellationToken cancellationToken = default);
}
