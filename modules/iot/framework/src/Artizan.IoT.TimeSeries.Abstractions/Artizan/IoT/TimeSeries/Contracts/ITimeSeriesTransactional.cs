using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据事务接口
/// 设计思路：适配支持事务的存储引擎，统一事务接口
/// 设计模式：事务模式（Transaction Pattern）
/// 设计考量：
/// 1. 部分时序库（如TimescaleDB）支持事务，需统一接口
/// 2. 不支持事务的引擎需抛出明确异常，避免误用
/// 3. 事务接口需支持异步释放，符合.NET最佳实践
/// </summary>
public interface ITimeSeriesTransactional : ITimeSeriesStorageContract
{
    /// <summary>
    /// 开启事务
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事务对象</returns>
    Task<ITimeSeriesTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
