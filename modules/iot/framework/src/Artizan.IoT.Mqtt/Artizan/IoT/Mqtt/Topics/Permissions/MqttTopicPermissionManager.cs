using Artizan.IoT.Mqtt.Topics.Permissions.Validators;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Topics.Permissions;

/// <summary>
/// 权限管理器默认实现
/// - 组合校验：组合多个校验器，按优先级执行
/// - 分层校验：基础层必须过，规则层仅明确拒绝时才拒绝
/// </summary>
public class DefaultMqttTopicPermissionManager : IMqttTopicPermissionManager, ISingletonDependency
{
    private readonly ILogger<DefaultMqttTopicPermissionManager> _logger;
    private readonly IEnumerable<IMqttTopicPermissionValidator> _validators;

    public DefaultMqttTopicPermissionManager(ILogger<DefaultMqttTopicPermissionManager> logger, IEnumerable<IMqttTopicPermissionValidator> validators)
    {
        _logger = logger;
        // 按优先级排序（基础层先执行）
        _validators = validators.OrderByDescending(v => v.Priority).ToList();
    }

    public async Task<MqttTopicPermissionResult> CheckPermissionAsync(MqttTopicPermissionContext context)
    {
        _logger.LogDebug("[{TrackId}][MQTT Topic 权限] 开始分层权限校验 | ClientId={ClientId} | Topic={Topic}",
            context.TrackId, context.ClientId, context.Topic);

        MqttTopicPermissionResult? finalResult = null;
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context);

            // 分层逻辑：
            // 1. 基础层（设备归属）失败 → 直接拒绝
            if (validator is DeviceOwnershipMqttTopicPermissionValidator && !result.IsAllowed)
            {
                _logger.LogWarning("[{TrackId}][MQTT Topic 权限] 基础层校验失败 | 原因={Reason}", context.TrackId, result.DenyReason);
                return result;
            }

            // 2. 规则层失败（明确拒绝）→ 直接拒绝
            if (validator is RuleMatchMqttTopicPermissionValidator && !result.IsAllowed)
            {
                _logger.LogWarning("[{TrackId}][MQTT Topic 权限] 规则层校验失败 | 原因={Reason}", context.TrackId, result.DenyReason);
                return result;
            }

            finalResult = result;
        }

        // 最终结果：基础层通过 + 规则层无拒绝 → 允许
        return finalResult ?? new MqttTopicPermissionResult
        {
            IsAllowed = true,
            DenyReason = string.Empty
        };
    }
}
