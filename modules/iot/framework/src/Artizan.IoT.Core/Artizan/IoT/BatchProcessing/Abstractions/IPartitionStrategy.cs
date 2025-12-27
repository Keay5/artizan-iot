namespace Artizan.IoT.BatchProcessing.Abstractions;

/// <summary>
/// 分区策略接口
/// 【设计思路】：封装不同的消息分区算法，支持动态替换
/// 【设计考量】：
/// 1. 消息分区是高并发批处理的核心，需适配不同分区规则（哈希/范围/自定义）
/// 2. 入参包含总分区数，支持动态扩缩容场景下的分区计算
/// 【设计模式】：策略模式（Strategy Pattern）
/// </summary>
public interface IPartitionStrategy
{
    /// <summary>
    /// 获取消息对应的分区Key
    /// </summary>
    /// <param name="message">待分区消息</param>
    /// <param name="partitionCount">总分区数</param>
    /// <returns>分区标识（如：partition_1）</returns>
    string GetPartitionKey(object message, int partitionCount);
}
