using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Topics.Permissions;

/// <summary>
/// 权限校验结果
/// </summary>
public class MqttTopicPermissionResult
{
    /// <summary>
    /// 是否通过校验
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// 拒绝原因（IsAllowed=false时必填）
    /// </summary>
    public string DenyReason { get; set; } = string.Empty;

    /// <summary>
    /// 匹配的规则（便于审计/日志）
    /// </summary>
    public MqttTopicPermissionRule? MatchedRule { get; set; }
}
