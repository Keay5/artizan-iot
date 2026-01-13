using System;
using System.Threading.Tasks;

namespace Artizan.IoT.ScriptDataCodec;

/// <summary>
/// 数据编解码核心接口
/// 设计模式：接口隔离原则（ISP），定义最小功能契约
/// 设计思路：抽象双向编解码能力，与具体语言实现解耦
/// 核心能力：Decode（原始→协议）、Encode（协议→原始）
/// </summary>
public interface IDataCodec : IDisposable
{
    /// <summary>
    /// 解码：原始二进制数据 → 标准协议数据（如JSON）
    /// </summary>
    /// <param name="context">执行上下文（含原始数据、方法名）</param>
    Task<ScriptExecutionResult> DecodeAsync(ScriptExecutionContext context);

    /// <summary>
    /// 编码：标准协议数据 → 原始二进制数据
    /// </summary>
    /// <param name="context">执行上下文（含协议数据、方法名）</param>
    Task<ScriptExecutionResult> EncodeAsync(ScriptExecutionContext context);

    /// <summary>
    /// 编解码器是否已释放
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// 尝试占用实例（线程安全）
    /// </summary>
    bool TryAcquire();

    /// <summary>
    /// 释放实例占用状态
    /// </summary>
    void Release();
}
