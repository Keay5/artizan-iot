using Artizan.IoT.Mqtts.Etos;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Signs;
using Artizan.IoT.Mqtts.Topics;
using Artizan.IoT.Mqtts.Topics.Permissions;
using Artizan.IoTHub.Localization;
using Artizan.IoTHub.Mqtts.Servers;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Mqtts.Services;

[ExposeServices(typeof(IMqttSubscriptionService), typeof(IMqttService))]
public class MqttSubscriptionService : MqttServiceBase, IMqttSubscriptionService, ISingletonDependency
{
    protected ILogger<MqttSubscriptionService> Logger { get; }
    protected IStringLocalizer<IoTHubResource> Localizer { get; }
    protected IGuidGenerator GuidGenerator { get; }
    protected MqttTopicTemplateParser MqttTopicParser { get; }
    protected IMqttTopicPermissionManager MqttTopicPermissionManager { get; }
    protected IDistributedEventBus DistributedEventBus { get; }

    public MqttSubscriptionService(
        ILogger<MqttSubscriptionService> logger,
        IStringLocalizer<IoTHubResource> localizer,
        IGuidGenerator guidGenerator,
        MqttTopicTemplateParser mqttTopicParser,
        IMqttTopicPermissionManager mqttPermissionManager,
        IDistributedEventBus distributedEventBus)
       : base()
    {
        Logger = logger;
        GuidGenerator = guidGenerator;
        MqttTopicParser = mqttTopicParser;
        MqttTopicPermissionManager = mqttPermissionManager;
        DistributedEventBus = distributedEventBus;
        Localizer = localizer;
    }

    public override void ConfigureMqttServer(MqttServer mqttServer)
    {
        base.ConfigureMqttServer(mqttServer);

        /* 不管是订阅还是发布，都是先执行拦截，再处理订阅/发布 */
        MqttServer.InterceptingSubscriptionAsync += InterceptingSubscriptionHandlerAsync;
        MqttServer.ClientSubscribedTopicAsync += ClientSubscribedTopicHandlerAsync;
        MqttServer.InterceptingUnsubscriptionAsync += InterceptingUnsubscriptionHandlerAsync;
        MqttServer.ClientUnsubscribedTopicAsync += ClientUnsubscribedTopicHandlerAsync;
    }

    /// <summary>
    /// 拦截客户端订阅
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task InterceptingSubscriptionHandlerAsync(InterceptingSubscriptionEventArgs eventArgs)
    {
        var trackId = GuidGenerator.Create().ToString();
        var clientId = eventArgs.ClientId;
        var topic = eventArgs.TopicFilter.Topic;

        try
        {
            // 1. 基础校验（快速失败）减少后续处理
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(topic))
            {
                Logger.LogWarning("[{TrackId}][Topic订阅] 参数无效| ClientId={0} | Topic={1}", trackId, clientId, topic);
                eventArgs.ProcessSubscription = false;
                return;
            }

            // 2. 获取设备认证信息（从会话缓存，避免重复解析）
            // 设计原因：MQTT会话会缓存设备认证信息，无需每次重新解析，提升性能
            MqttAuthParams? authParams = eventArgs.SessionItems[AuthParamsSessionItemKey] as MqttAuthParams;
            if (authParams == null)
            {
                Logger.LogWarning("[{TrackId}][Topic订阅] 未找到认证信息 | ClientId={0}", trackId, clientId);
                eventArgs.ProcessSubscription = false;
                return;
            }

            // 3. 设备信息校验：保证消息关联的设备信息完整
            if (string.IsNullOrWhiteSpace(authParams.ProductKey) || string.IsNullOrWhiteSpace(authParams.DeviceName))
            {
                Logger.LogWarning("[{TrackId}][Topic订阅] 设备信息不完整 | ProductKey={PK} | DeviceName={DN}",
                    trackId, authParams.ProductKey, authParams.DeviceName);
                eventArgs.ProcessSubscription = false;
                return;
            }

            // 4.  权限校验（核心切入：订阅前）
            var permissionContext = new MqttTopicPermissionContext
            {
                TrackId = trackId,
                ClientId = clientId,
                AuthProductKey = authParams.ProductKey,
                AuthDeviceName = authParams.DeviceName,
                Topic = topic,
                Operation = MqttTopicOperation.Subscribe
            };

            var permissionResult = await MqttTopicPermissionManager.CheckPermissionAsync(permissionContext);
            if (!permissionResult.IsAllowed)
            {
                Logger.LogWarning("[{TrackId}][MQTT Topic 订阅权限] 校验失败 | ClientId={ClientId} | Topic={Topic} | 原因={Reason}",
                    trackId, clientId, topic, permissionResult.DenyReason);
                eventArgs.ProcessSubscription = false;
                return;
            }

            // 订阅校验通过
            Logger.LogDebug("[{TrackId}][MQTT Topic 订阅权限] 校验通过 | ClientId={ClientId} | Topic={Topics}",
                trackId, clientId, topic);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{TrackId}][Topic订阅] 发生异常 | ClientId={ClientId}", trackId, clientId);
            eventArgs.ProcessSubscription = false;
        }
    }

    /// <summary>
    /// 处理客户端订阅
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task ClientSubscribedTopicHandlerAsync(ClientSubscribedTopicEventArgs eventArgs)
    {
        var trackId = GuidGenerator.Create().ToString();
        var topic = eventArgs.TopicFilter.Topic;
        var clientId = eventArgs.ClientId;

        MqttAuthParams? authParams = eventArgs.SessionItems[AuthParamsSessionItemKey] as MqttAuthParams;
        var productKey = authParams?.ProductKey;
        var deviceName = authParams?.DeviceName;

        Check.NotNull(clientId, nameof(clientId));
        Check.NotNull(topic, nameof(topic));
        Check.NotNull(productKey, nameof(productKey));
        Check.NotNull(deviceName, nameof(deviceName));

        try
        {

            //(await VerifyClientCanSubscribeTopicAsync(
            //    topic: topic,
            //    productKey: productKey,
            //    deviceName: deviceName)
            // ).CheckErrors();

            await DistributedEventBus.PublishAsync(
               new MqttClientSubscribedTopicEto
               {
                   MqttTrackId = trackId,
                   MqttClientId = clientId,
                   MqttTopic = topic,
                   ProductKey = productKey,
                   DeviceName = deviceName
               }
            );

            Logger.LogInformation(
               "[{TrackId}][MQTT Topic 订阅] 订阅成功 | 设备={PK}/{DN} | 主题={Topic}",
               trackId, authParams.ProductKey, authParams.DeviceName, topic
            );
        }
        catch (Exception ex)
        {
            Logger.LogInformation(
               "[{TrackId}][MQTT Topic 订阅] 发生异常 | 设备={PK}/{DN} | 主题={Topic}",
               trackId, authParams.ProductKey, authParams.DeviceName, topic
            );
            Logger.LogException(ex);
        }

    }

    /// <summary>
    /// 拦截取消订阅
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Task InterceptingUnsubscriptionHandlerAsync(InterceptingUnsubscriptionEventArgs eventArgs)
    {
        Logger.LogInformation($"Intercepting MQTT Client unsubscribe topic > clientId:{eventArgs.ClientId}, topic:{eventArgs.Topic}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理客户端取消订阅
    /// </summary>
    /// <param name="eventArgs "></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task ClientUnsubscribedTopicHandlerAsync(ClientUnsubscribedTopicEventArgs eventArgs)
    {
        var trackId = GuidGenerator.Create().ToString();
        var clientId = eventArgs.ClientId;
        var topic = eventArgs.TopicFilter;

        MqttAuthParams? authParams = eventArgs.SessionItems[AuthParamsSessionItemKey] as MqttAuthParams;
        var productKey = authParams?.ProductKey;
        var deviceName = authParams?.DeviceName;


        Check.NotNull(clientId, nameof(clientId));
        Check.NotNull(topic, nameof(topic));
        Check.NotNull(productKey, nameof(productKey));
        Check.NotNull(deviceName, nameof(deviceName));

        await DistributedEventBus.PublishAsync(
          new MqttClientUnsubscribedTopicEto
          {
              MqttTrackId = trackId,
              MqttClientId = clientId,
              MqttTopic = topic,
              ProductKey = productKey,
              DeviceName = deviceName
          }
        );

        Logger.LogInformation($"[{trackId}][MQTT Topic 订阅] 取消订阅主题成功，设备：{authParams?.ProductKey}/{authParams?.DeviceName}, 主题：{topic}, clientId:{eventArgs.ClientId}");
    }
}
