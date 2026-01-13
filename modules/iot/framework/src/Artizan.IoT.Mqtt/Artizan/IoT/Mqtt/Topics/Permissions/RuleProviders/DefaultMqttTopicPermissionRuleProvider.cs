using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Topics.Permissions.RuleProviders;

/// <summary>
/// 默认权限规则提供器（基于静态配置，可扩展为数据库/Redis实现）
/// </summary>
public class DefaultMqttTopicPermissionRuleProvider : IMqttTopicPermissionRuleProvider, ISingletonDependency
{
    private readonly ILogger<DefaultMqttTopicPermissionRuleProvider> _logger;
    private List<MqttTopicPermissionRule> _cachedRules = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public DefaultMqttTopicPermissionRuleProvider(ILogger<DefaultMqttTopicPermissionRuleProvider> logger)
    {
        _logger = logger;
        // 初始化默认规则（可替换为从appsettings.json/数据库加载）
        InitDefaultRules();
    }

    /// <summary>
    /// 初始化默认规则：
    /// 1. 设备仅允许操作自己的Topic（/sys/${pk}/${dn}/#）
    /// 2. 系统级Topic（/sys/${pk}/${dn}/system/#）仅允许平台发布，设备禁止发布
    /// </summary>
    private void InitDefaultRules()
    {
        _cachedRules = new List<MqttTopicPermissionRule>
        {
            // 规则1：拒绝设备发布系统级Topic（高优先级）
            new MqttTopicPermissionRule
            {
                Identifier = "sys_topic_publish_deny",
                TopicPattern = "/sys/+/+/system/#",
                Operation = MqttTopicOperation.Publish,
                Allow = false,
                Priority = 80,
                IsSystemRule = true,
                Description = "设备禁止发布系统级Topic"
            },
            // 规则2：允许设备操作自己的Topic（发布+订阅）（兜底规则）
            new MqttTopicPermissionRule
            {
                Identifier = "device_own_topic_allow",
                TopicPattern = "/sys/${productKey}/${deviceName}/#",
                Operation = MqttTopicOperation.Publish | MqttTopicOperation.Subscribe,
                Allow = true,
                Priority = 90, // 低于设备归属校验（100），高于系统拒绝规则（80）
                Description = "允许设备操作自己的Topic（兜底规则）"
            }
        };
        _logger.LogInformation("[MQTT Topic 权限] | 默认MQTT权限规则初始化完成，共{Count}条规则 | 兜底规则Identifier={Identifier}",
            _cachedRules.Count, "device_own_topic_allow");
    }

    public async Task<List<MqttTopicPermissionRule>> GetRulesAsync()
    {
        return await Task.FromResult(_cachedRules.OrderByDescending(r => r.Priority).ToList());
    }

    public async Task RefreshRulesAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            _logger.LogInformation("[MQTT Topic 权限] 开始刷新MQTT权限规则");
            InitDefaultRules(); // 实际场景替换为从数据源重新加载
            _logger.LogInformation("[MQTT Topic 权限] MQTT权限规则刷新完成，共{Count}条规则", _cachedRules.Count);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
