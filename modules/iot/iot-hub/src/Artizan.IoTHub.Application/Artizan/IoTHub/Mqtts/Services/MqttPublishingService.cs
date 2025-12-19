using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Signs;
using Artizan.IoT.Mqtts.Topics;
using Artizan.IoT.Mqtts.Topics.Permissions;
using Artizan.IoT.Mqtts.Topics.Routes;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Mqtts.Servers;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MQTTnet;
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
    protected MqttMessageDispatcher MqttMessageDispatcher { get; }
    protected IMqttMessageRouter MqttMessageRouter { get; }

    public MqttPublishingService(
        ILogger<MqttPublishingService> logger,
        IStringLocalizer<IoTHubResource> localizer,
        IGuidGenerator guidGenerator,
        MqttTopicTemplateParser mqttTopicParser,
        IMqttTopicPermissionManager mqttPermissionManager,
        MqttMessageDispatcher mqttMessageDispatcher,
        IMqttMessageRouter mqttMessageRouter)
       : base()
    {
        Logger = logger;
        Localizer = localizer;
        GuidGenerator = guidGenerator;
        MqttTopicParser = mqttTopicParser;
        MqttTopicPermissionManager = mqttPermissionManager;
        MqttMessageDispatcher = mqttMessageDispatcher;
        MqttMessageRouter = mqttMessageRouter;
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
        var payloadSegment = eventArgs.ApplicationMessage.PayloadSegment;

        // 1. 基础校验（快速失败）：无效消息直接拒绝，减少后续处理
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(topic) ||
            payloadSegment.Count == 0)
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 无效消息 | ClientId={0} | Topic={1}", trackId, clientId, topic);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 2. 获取设备认证信息（从会话缓存，避免重复解析）
        // 设计原因：MQTT会话会缓存设备认证信息，无需每次重新解析，提升性能
        MqttAuthParams? authParams = eventArgs.SessionItems[AuthParamsSessionItemKey] as MqttAuthParams;
        if (authParams == null)
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 未找到认证信息 | ClientId={0}", trackId, clientId);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 3. 设备信息校验：保证消息关联的设备信息完整
        if (string.IsNullOrWhiteSpace(authParams.ProductKey) ||
            string.IsNullOrWhiteSpace(authParams.DeviceName))
        {
            Logger.LogWarning("[{TrackId}][MQTT Topic 发布] 设备信息不完整 | ProductKey={PK} | DeviceName={DN}",
                trackId, authParams.ProductKey, authParams.DeviceName);
            eventArgs.ProcessPublish = false;
            return;
        }

        // 4. 权限校验（核心切入：发布前）
        var permissionContext = new MqttTopicPermissionContext
        {
            TrackId = trackId,
            ClientId = clientId,
            AuthProductKey = authParams.ProductKey,
            AuthDeviceName = authParams.DeviceName,
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
                trackId, authParams.ProductKey, authParams.DeviceName, topic, payloadSegment.Count
            );
        }

        //// 5. 异步处理消息（不阻塞MQTT服务器主线程）
        //await ProcessSingleMessageAsync(eventArgs.ApplicationMessage, eventArgs.ClientId, authParams.ProductKey, authParams.DeviceName, trackId);

        //Logger.LogDebug(
        //    "[处理设备消息] [完成] | [{TrackId}] | 设备={0}/{1} | 主题={2} | 大小={3}B",
        //    trackId, authParams.ProductKey, authParams.DeviceName, topic, payloadSegment.Count
        //);

        await ProcessSingleMessageAsync(eventArgs.ApplicationMessage, eventArgs.ClientId, trackId);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug(
                "[{TrackId}][MQTT Topic 发布] 发布完成 | 设备={PK}/{DN} | 主题={Topic} | 大小={Size}B",
                trackId, authParams.ProductKey, authParams.DeviceName, topic, payloadSegment.Count
            );
        }
    }

    // MqttPublishingService 中消费消息时创建上下文
    private async Task ProcessSingleMessageAsync(MqttApplicationMessage mqttMessage, string mqttClientId, string trackId)
    {
        //// 创建上下文（唯一入口，初始化基础层）
        //var mqttMessageContext = new MqttMessageContext(mqttMessage.ToMqttRawMessage(), mqttClientId, productKey, deviceName, trackId);
        //await MqttMessageDispatcher.EnqueueMessageAsync(mqttMessageContext);


        // 路由系统核心载体：初期构造函数（未解析ProductKey/DeviceKey，路由系统后续填充）
        var mqttMessageContext = new MqttMessageContext(mqttMessage.ToMqttRawMessage(), mqttClientId, trackId);
        // 调用路由系统分发消息（核心步骤）
        await MqttMessageRouter.RouteMessageAsync(mqttMessageContext);
    }
}
