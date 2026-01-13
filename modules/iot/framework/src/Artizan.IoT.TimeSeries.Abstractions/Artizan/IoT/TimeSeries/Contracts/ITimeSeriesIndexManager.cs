using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 时序数据索引管理接口
/// 设计思路：抽象索引管理操作，适配不同引擎的索引特性
/// 设计模式：策略模式（Strategy Pattern）
/// 设计考量：
/// 1. 不同时序库索引语法不同，通过接口统一
/// 2. 支持创建/删除索引，优化查询性能
/// 3. 索引策略可插拔，便于扩展
/// </summary>
public interface ITimeSeriesIndexManager : ITimeSeriesStorageContract
{
    /// <summary>
    /// 创建索引
    /// </summary>
    /// <param name="strategies">索引策略列表</param>
    /// <param name="measurement">测量表名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    Task CreateIndexAsync(
        IEnumerable<ITimeSeriesIndexStrategy> strategies,
        string measurement,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除索引
    /// </summary>
    /// <param name="indexName">索引名称</param>
    /// <param name="measurement">测量表名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    Task DropIndexAsync(
        string indexName,
        string measurement,
        CancellationToken cancellationToken = default);
}

