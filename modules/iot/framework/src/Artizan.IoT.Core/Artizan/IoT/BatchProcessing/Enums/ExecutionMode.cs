using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Enums;

/// <summary>
/// 批处理执行模式枚举
/// 【设计思路】：区分串行/并行执行逻辑，适配不同一致性要求的业务
/// 【设计考量】：
/// 1. 串行：保证消息处理顺序，适合账务、交易等强一致性场景
/// 2. 并行：提升处理效率，适合日志、IoT数据等弱一致性场景
/// 【设计模式】：简单枚举（无设计模式，纯语义封装）
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// 串行执行（单线程处理单个分区消息）
    /// </summary>
    Serial,

    /// <summary>
    /// 并行执行（多线程处理单个分区消息）
    /// </summary>
    Parallel
}
