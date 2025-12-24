using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages;

/// <summary>
/// MQTT消息处理步骤（并行执行）
/// 工作流步骤接口（适配并行操作：转发 / 规则引擎 / 联动）
/// 抽象所有并行执行的业务步骤，新增步骤仅需实现接口
/// </summary>
public interface IMqttWorkflowStep
{
    /// <summary>
    /// 步骤标识（唯一，用于日志/监控）
    /// </summary>
    string StepIdentifier { get; }

    /// <summary>
    /// 是否启用（配置化控制）
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 执行步骤（带Polly容错策略）
    /// </summary>
    Task ExecuteAsync(MqttMessageContext context);
}
