using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Topics.Permissions;

/// <summary>
/// MQTT Topic权限管理器（统一入口，组合规则提供器+校验器）
/// 【设计】：门面模式，对外提供简单入口，隐藏内部复杂逻辑
/// </summary>
public interface IMqttTopicPermissionManager
{
    /// <summary>
    /// 统一权限校验入口
    /// </summary>
    Task<MqttTopicPermissionResult> CheckPermissionAsync(MqttTopicPermissionContext context);
}
