using Artizan.IoT.Mqtts.Topics.Permissions.RuleProviders;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics.Permissions.Validators;

/// <summary>
///规则匹配校验器（规则层：基于规则库匹配Topic操作权限）
/// </summary>
//[ExposeServices(typeof(IMqttTopicPermissionValidator))] //当名称实现类的名称不满足自动依赖注入的类名约定时，显式暴露服务接口
public class RuleMatchMqttTopicPermissionValidator : IMqttTopicPermissionValidator, ISingletonDependency
{
    private readonly ILogger<RuleMatchMqttTopicPermissionValidator> _logger;
    private readonly IMqttTopicPermissionRuleProvider _ruleProvider;

    public RuleMatchMqttTopicPermissionValidator(
        ILogger<RuleMatchMqttTopicPermissionValidator> logger,
        IMqttTopicPermissionRuleProvider ruleProvider)
    {
        _logger = logger;
        _ruleProvider = ruleProvider;
    }

    /// <summary>
    /// 次优先级：设备归属校验通过后执行（规则匹配层，低于基础合法性层(<seealso cref="DeviceOwnershipMqttTopicPermissionValidator.Priority"/>)）
    /// </summary>
    public int Priority => 50;

    public async Task<MqttTopicPermissionResult> ValidateAsync(MqttTopicPermissionContext context)
    {
        var rules = await _ruleProvider.GetRulesAsync();
        var targetRules = rules
            .Where(r => r.Operation == context.Operation)
            .OrderByDescending(r => r.Priority) // 按优先级匹配
            .ToList();

        // 标记是否匹配到允许规则（有一条允许即通过）
        bool hasAllowRule = false;
        MqttTopicPermissionRule? matchedAllowRule = null;

        foreach (var rule in targetRules)
        {
            var compareResult = MqttTopicFilterComparer.Compare(context.Topic, rule.TopicPattern);

            switch (compareResult)
            {
                case MqttTopicFilterCompareResult.IsMatch:
                    if (rule.Allow)
                    {
                        // 匹配到允许规则 → 直接标记通过
                        hasAllowRule = true;
                        matchedAllowRule = rule;
                        break; // 有一条允许即可，无需继续匹配
                    }
                    else
                    {
                        // 匹配到拒绝规则 → 直接返回拒绝
                        var denyReason = rule.Description ?? $"匹配拒绝规则[{rule.Identifier}]：禁止{context.Operation}操作Topic";
                        _logger.LogWarning("[{TrackId}][MQTT Topic 权限] | 路由权限校验器= {Validator} | {DenyReason}", context.TrackId, nameof(RuleMatchMqttTopicPermissionValidator), denyReason);
                        return new MqttTopicPermissionResult
                        {
                            IsAllowed = false,
                            DenyReason = denyReason,
                            MatchedRule = rule
                        };
                    }

                case MqttTopicFilterCompareResult.FilterInvalid:
                    _logger.LogWarning("[{TrackId}][MQTT Topic 权限] 规则模板非法 | Identifier={Identifier} | Pattern={Pattern} | 路由权限校验器= {Validator}",
                        context.TrackId, rule.Identifier, rule.TopicPattern, nameof(RuleMatchMqttTopicPermissionValidator));
                    continue;

                case MqttTopicFilterCompareResult.TopicInvalid:
                    var topicInvalidReason = $"待校验Topic非法 | Topic={context.Topic}";
                    _logger.LogWarning("[{TrackId}][MQTT Topic 权限] {topicInvalidReason} | 路由权限校验器= {Validator}", context.TrackId, topicInvalidReason, nameof(RuleMatchMqttTopicPermissionValidator));
                    return new MqttTopicPermissionResult
                    {
                        IsAllowed = false,
                        DenyReason = topicInvalidReason
                    };

                default:
                    // 无匹配 → 继续下一条规则
                    continue;
            }
        }

        // 最终判断：1.有允许规则 → 通过；2.无规则 → 基础层通过则默认允许
        if (hasAllowRule)
        {
            _logger.LogDebug("[{TrackId}][MQTT Topic 权限] 匹配允许规则 | Identifier={Identifier} | Topic={Topic} | 路由权限校验器= {Validator}",
                context.TrackId, matchedAllowRule!.Identifier, context.Topic, nameof(RuleMatchMqttTopicPermissionValidator));
            return new MqttTopicPermissionResult
            {
                IsAllowed = true,
                DenyReason = string.Empty,
                MatchedRule = matchedAllowRule
            };
        }
        else // 宽松策略:无匹配规则则默认允许
        {
            _logger.LogDebug("[{TrackId}][MQTT Topic 权限] 无匹配拒绝规则，默认允许 | Topic={Topic} | 路由权限校验器= {Validator}",
                context.TrackId, context.Topic, nameof(RuleMatchMqttTopicPermissionValidator));
            return new MqttTopicPermissionResult
            {
                IsAllowed = true,
                DenyReason = string.Empty
            };
        }
    }
}
