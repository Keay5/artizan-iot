namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 完整仓储接口（组合所有能力）
/// 设计思路：组合读写、事务、索引管理接口，提供完整能力
/// 设计模式：接口组合模式（Interface Composition）
/// 设计考量：
/// 1. 上层应用可直接依赖此接口，获取完整能力
/// 2. 底层实现可按需实现不同子接口
/// 3. 符合里氏替换原则，可替换不同存储实现
/// </summary>
public interface ITimeSeriesDataRepository : ITimeSeriesDataReader, ITimeSeriesDataWriter, ITimeSeriesTransactional, ITimeSeriesIndexManager
{
}
