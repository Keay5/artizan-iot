using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Topics.Permissions.Validators;

/// <summary>
/// MQTT Topic权限校验器（抽象校验逻辑，支持多实现/组合校验）
/// 【设计】：策略模式，可扩展不同校验逻辑（设备归属/系统Topic/自定义）
/// </summary>
public interface IMqttTopicPermissionValidator
{
    /// <summary>
    /// 校验器优先级（多校验器时按优先级执行，数值越大越先执行）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 校验权限
    /// </summary>
    Task<MqttTopicPermissionResult> ValidateAsync(MqttTopicPermissionContext context);


}
