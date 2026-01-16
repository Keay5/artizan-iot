using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Topics.Routes;


/// <summary>
/// MQTT Topic 路由特性
/// 作用：标记 <see cref="IMqttMessageHandler"/> 支持的 Topic 模板，用于路由自动注册
/// 设计思路：通过特性将 Handler 与 Topic 模板强绑定，支持多模板、优先级配置，无需手动注册路由
/// </summary>
/// <example>
/// 单 Topic 路由示例：
///     [MqttTopicRoute("/sys/${productKey}/${deviceName}/event/property/post", Priority = 10)]
///     public class PropertyReportHandler : SafeMqttMessageHandler { ... }
/// 
/// 多 Topic 路由示例：
///     [MqttTopicRoute("/ota//device/upgrade/${productKey}/${deviceName}/event/property/post", Priority = 20)]
///     [MqttTopicRoute("/ota//device/progress/${productKey}/${deviceName}/event/property/post", Priority = 20)]
///     public class PropertyReportHandler : SafeMqttMessageHandler { ... }
///     
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class MqttTopicRouteAttribute : Attribute
{
    /// <summary>
    /// Topic模板（支持${占位符}和MQTT标准通配符+/#）
    /// 格式示例：/sys/${productKey}/${deviceName}/#、/ota/${productKey}/+
    /// </summary>
    public string TopicTemplate { get; }

    /// <summary>
    /// 路由优先级（数值越大，匹配时越优先）
    /// 默认值：0；系统内置Topic建议设为-100，自定义Topic设为10+
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 是否启用当前路由规则（默认true）
    /// 用途：临时禁用路由，无需修改代码或配置
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 构造函数（必填Topic模板）
    /// </summary>
    public MqttTopicRouteAttribute(string topicTemplate)
    {
        TopicTemplate = topicTemplate ?? throw new ArgumentNullException(nameof(topicTemplate), "Topic模板不能为空");
    }
}

