using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics.Permissions.Validators;

/// <summary>
/// 设备归属校验器（校验Topic中的PK/DN是否与认证的一致）
/// </summary>
//[ExposeServices(typeof(IMqttTopicPermissionValidator))] //当名称实现类的名称不满足自动依赖注入的类名约定时，显式暴露服务接口
public class DeviceOwnershipMqttTopicPermissionValidator : IMqttTopicPermissionValidator, ISingletonDependency
{
    private readonly ILogger<DeviceOwnershipMqttTopicPermissionValidator> _logger;
    private readonly MqttTopicTemplateParser _topicTemplateParser; // 复用路由层的Topic解析器

    public DeviceOwnershipMqttTopicPermissionValidator(
        ILogger<DeviceOwnershipMqttTopicPermissionValidator> logger, 
        MqttTopicTemplateParser topicTemplateParser)
    {
        _logger = logger;
        _topicTemplateParser = topicTemplateParser;
    }

    // 最高优先级：基础合法性校验必须先执行
    public int Priority => 100;

    public async Task<MqttTopicPermissionResult> ValidateAsync(MqttTopicPermissionContext context)
    {
        // 1. 定义设备级Topic模板（复用路由层解析逻辑）
        var deviceTopicTemplate = "/sys/${productKey}/${deviceName}/#";
        var parseResult = _topicTemplateParser.Parse(deviceTopicTemplate);
        var topicMatch = parseResult.TemplateRegex.Match(context.Topic);

        // 非设备级Topic（如平台级/广播Topic），跳过归属校验
        if (!topicMatch.Success)
        {
            _logger.LogDebug("[{TrackId}][MQTT Topic 权限] 非设备级Topic，跳过归属校验 | 路由权限校验器= {Validator} | Topic={Topic}  ",
                context.TrackId, nameof(DeviceOwnershipMqttTopicPermissionValidator), context.Topic);
            return await Task.FromResult(new MqttTopicPermissionResult
            {
                IsAllowed = true,
                DenyReason = string.Empty
            });
        }

        // 2. 提取Topic中的PK/DN
        var parsedProductKey = topicMatch.Groups["productKey"]?.Value;
        var parsedDeviceName = topicMatch.Groups["deviceName"]?.Value;

        // 3. 比对认证上下文的PK/DN
        if (parsedProductKey != context.AuthProductKey || parsedDeviceName != context.AuthDeviceName)
        {
            var denyReason = $"设备归属校验失败：认证PK={context.AuthProductKey},认证DN={context.AuthDeviceName} | Topic解析PK={parsedProductKey},Topic解析DN={parsedDeviceName}";
            _logger.LogWarning("[{TrackId}][MQTT Topic 权限] | 路由权限校验器= {Validator} |{DenyReason}", context.TrackId, nameof(DeviceOwnershipMqttTopicPermissionValidator), denyReason);
            return await Task.FromResult(new MqttTopicPermissionResult
            {
                IsAllowed = false,
                DenyReason = denyReason
            });
        }

        // 归属校验通过
        _logger.LogDebug("[{TrackId}][MQTT Topic 权限] 设备归属校验通过 | 路由权限校验器= {Validator} | PK={PK} | DN={DN}",
            context.TrackId, nameof(DeviceOwnershipMqttTopicPermissionValidator), context.AuthProductKey, context.AuthDeviceName);
        return await Task.FromResult(new MqttTopicPermissionResult
        {
            IsAllowed = true,
            DenyReason = string.Empty
        });
    }
}
