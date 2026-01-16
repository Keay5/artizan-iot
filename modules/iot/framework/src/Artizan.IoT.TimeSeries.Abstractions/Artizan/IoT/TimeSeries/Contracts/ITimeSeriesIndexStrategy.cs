using System.Collections.Generic;

namespace Artizan.IoT.TimeSeries.Contracts;

/// <summary>
/// 索引策略接口
/// 设计思路：封装索引创建逻辑，支持不同引擎的差异化实现
/// 设计模式：策略模式（Strategy Pattern）
/// 设计考量：
/// 1. 索引名称和字段标准化
/// 2. 生成特定引擎的创建语句
/// 3. 便于扩展新的索引策略
/// </summary>
public interface ITimeSeriesIndexStrategy
{
    /// <summary>
    /// 索引名称
    /// </summary>
    string IndexName { get; }

    /// <summary>
    /// 索引字段列表
    /// </summary>
    IList<string> IndexFields { get; }

    /// <summary>
    /// 生成创建索引的命令
    /// </summary>
    /// <param name="measurement">测量表名</param>
    /// <returns>索引创建命令</returns>
    string GenerateCreateIndexCommand(string measurement);
}
