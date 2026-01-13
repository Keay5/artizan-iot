using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Messages;

/// <summary>
/// 单个步骤的执行结果（上下文嵌套对象，记录每一步的状态）
/// </summary>
public class MessageProcessResult
{
    /// <summary>
    /// 步骤名称（如DataForward/RuleEngine）
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 执行是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（失败时填充）
    /// </summary>
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// 异常详情（失败时填充，便于排查）
    /// </summary>
    public string? ExceptionDetail { get; set; }

    /// <summary>
    /// 步骤执行耗时（性能分析）
    /// </summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 步骤执行时间（时序排查）
    /// </summary>
    public DateTime ExecuteTime { get; set; } = DateTime.Now;
}
