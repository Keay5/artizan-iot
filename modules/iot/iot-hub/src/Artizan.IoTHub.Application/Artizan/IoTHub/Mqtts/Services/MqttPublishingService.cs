using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Mqtt.Auths;
using Artizan.IoT.Mqtt.Messages;
using Artizan.IoT.Mqtt.Messages.Dispatchers;
using Artizan.IoT.Mqtt.Topics.Parsings;
using Artizan.IoT.Mqtt.Topics.Permissions;
using Artizan.IoTHub.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions;
using MQTTnet.Server;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Mqtts.Services;

[ExposeServices(typeof(IMqttPublishingService), typeof(IMqttService))] // ABP依赖注入：暴露服务接口
public class MqttPublishingService : MqttServiceBase, IMqttPublishingService, ISingletonDependency
{
    protected ILogger<MqttPublishingService> Logger { get; }
    protected IStringLocalizer<IoTHubResource> Localizer { get; }
    protected IGuidGenerator GuidGenerator { get; }
    protected MqttTopicTemplateParser MqttTopicParser { get; }
    protected IMqttTopicPermissionManager MqttTopicPermissionManager { get; }
    protected IMqttMessageDispatcher MqttMessageDispatcher { get; }

    public MqttPublishingService(
        ILogger<MqttPublishingService> logger,
        IStringLocalizer<IoTHubResource> localizer,
        IGuidGenerator guidGenerator,
        MqttTopicTemplateParser mqttTopicParser,
        IMqttTopicPermissionManager mqttPermissionManager,
        IMqttMessageDispatcher mqttMessageDispatcher)
       : base()
    {
        Logger = logger;
        Localizer = localizer;
        GuidGenerator = guidGenerator;
        MqttTopicParser = mqttTopicParser;
        MqttTopicPermissionManager = mqttPermissionManager;
        MqttMessageDispatcher = mqttMessageDispatcher;
    }

    public override void ConfigureMqttServer(MqttServer mqttServer)
    {
        base.ConfigureMqttServer(mqttServer);

        // 注册发布拦截器：所有MQTT Client 发布的消息都会经过此回调处理
        MqttServer.InterceptingPublishAsync += InterceptingPublishHandlerAsync;
    }

    private async Task InterceptingPublishHandlerAsync(InterceptingPublishEventArgs eventArgs)
    {
        // 生成唯一追踪ID：便于全链路日志追踪
        var trackId = GuidGenerator.Create().ToString();

        var clientId = eventArgs.ClientId;
        var topic = eventArgs.ApplicationMessage.Topic;
        var qosLevel = eventArgs.ApplicationMessage.QualityOfServiceLevel;
        var retain = eventArgs.ApplicationMessage.Retain;
        var payload = eventArgs.ApplicationMessage.Payload;

        // 1. 基础校验（快速失败）：无效消息直接拒绝，减少后续处理
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(topic) ||
            payload.Length == 0)
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 无效消息 | ClientId={0} | Topic={1}", trackId, clientId, topic);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 2. 获取设备认证信息（从会话缓存，避免重复解析）
        // 设计原因：MQTT会话会缓存设备认证信息，无需每次重新解析，提升性能
        MqttSignParams? signParams = eventArgs.SessionItems[signParamsSessionItemKey] as MqttSignParams;
        if (signParams == null)
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 未找到认证信息 | ClientId={0}", trackId, clientId);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 3. 设备信息校验：保证消息关联的设备信息完整
        if (string.IsNullOrWhiteSpace(signParams.ProductKey) ||
            string.IsNullOrWhiteSpace(signParams.DeviceName))
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 设备信息不完整 | ProductKey={PK} | DeviceName={DN}",
                trackId, signParams.ProductKey, signParams.DeviceName);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 4. 权限校验（核心切入：发布前）
        var permissionContext = new MqttTopicPermissionContext
        {
            TrackId = trackId,
            ClientId = clientId,
            AuthProductKey = signParams.ProductKey,
            AuthDeviceName = signParams.DeviceName,
            Topic = topic,
            Operation = MqttTopicOperation.Publish
        };
        var permissionResult = await MqttTopicPermissionManager.CheckPermissionAsync(permissionContext);
        if (!permissionResult.IsAllowed)
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 权限校验失败 | ClientId={ClientId} | Topic={Topic} | 原因={Reason}",
                trackId, clientId, topic, permissionResult.DenyReason);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 5. 分级日志（性能优化：Debug级别才输出详细信息，减少日志开销）
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug(
                "[{TrackId}][MQTT Topic 发布] 开始发布 | 设备={PK}/{DN} | 主题={Topic} | 大小={Size}B",
                trackId, signParams.ProductKey, signParams.DeviceName, topic, payload.Length
            );
        }

        // 6. 处理消息
        // 创建路由系统核心载体：初期构造函数（若未解析ProductKey/DeviceKey，路由系统后续填充）
        var mqttMessageContext = new MqttMessageContext(
            eventArgs.ApplicationMessage.ToMqttRawMessage(),
            clientId,
            signParams.ProductKey,
            signParams.DeviceName,
            trackId,
            eventArgs.CancellationToken
        );
        await ProcessSingleMessageAsync(mqttMessageContext);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug(
                "[{TrackId}][MQTT Topic 发布] 发布完成 | 设备={PK}/{DN} | 主题={Topic} | 大小={Size}B",
                trackId, signParams.ProductKey, signParams.DeviceName, topic, payload.Length
            );
        }
    }

    private async Task ProcessSingleMessageAsync(MqttMessageContext mqttMessageContext)
    {
        // 非高并发：调用路由系统分发消息（核心步骤）
        //await MqttMessageRouter.RouteMessageAsync(mqttMessageContext, mqttMessageContext.CancellationToken);

        // 高并发：入队到分发器（接管高并发）
        await MqttMessageDispatcher.EnqueueAsync(mqttMessageContext, mqttMessageContext.CancellationToken);
    }
}
