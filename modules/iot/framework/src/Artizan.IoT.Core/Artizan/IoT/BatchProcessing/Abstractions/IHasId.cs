namespace Artizan.IoT.BatchProcessing.Abstractions;
/// <summary>
/// 带ID的消息标记接口
/// 【设计思路】：标记接口（Marker Interface），标识消息包含唯一ID
/// 【设计考量】：范围分区策略需要基于消息ID进行分区计算
/// 【设计模式】：标记模式（Marker Pattern）
/// </summary>
public interface IHasId
{
    /// <summary>
    /// 消息唯一ID
    /// </summary>
    long Id { get; }
}
