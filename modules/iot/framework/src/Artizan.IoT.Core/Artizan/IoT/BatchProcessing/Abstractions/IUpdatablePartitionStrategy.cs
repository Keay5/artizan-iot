using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 可更新的分区策略接口
/// 【设计思路】：接口继承扩展，遵循「开闭原则」
/// 【设计考量】：动态扩缩容场景下，分区策略需要同步更新分区数
/// 【设计模式】：接口继承（基于开闭原则的扩展）
/// </summary>
public interface IUpdatablePartitionStrategy : IPartitionStrategy
{
    /// <summary>
    /// 更新分区数
    /// </summary>
    /// <param name="newCount">新分区数</param>
    /// <param name="traceId">追踪ID（便于日志排查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task UpdatePartitionCountAsync(int newCount, string traceId, CancellationToken cancellationToken);
}
