using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Topics.Permissions;

/// <summary>
/// MQTT Topic权限规则（支持动态配置、通配符）
/// </summary>
public class MqttTopicPermissionRule
{
    /// <summary>
    /// 规则唯一标识（业务维度，避免与数据库Id冲突）
    /// </summary>
    public string Identifier { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Topic匹配模板（支持MQTT通配符：+单级、#多级）
    /// 示例：/sys/${productKey}/${deviceName}/thing/event/+ 或 /sys/+/+/system/#
    /// </summary>
    public string TopicPattern { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型（发布/订阅）
    /// </summary>
    public MqttTopicOperation Operation { get; set; }

    /// <summary>
    /// 是否允许（true=允许，false=拒绝，拒绝规则优先级更高）
    /// </summary>
    public bool Allow { get; set; }

    /// <summary>
    /// 规则描述（便于运维）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级（数值越大，规则越先匹配）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 是否系统级规则（系统级Topic专用）
    /// </summary>
    public bool IsSystemRule { get; set; } = false;
}