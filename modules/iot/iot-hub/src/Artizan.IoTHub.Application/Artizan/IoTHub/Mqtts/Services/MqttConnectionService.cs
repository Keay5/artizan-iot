using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Mqtt.Auths;
using Artizan.IoT.Mqtt.Etos;
using Artizan.IoT.Thing;
using Artizan.IoTHub.Devices.Auths;
using Artizan.IoTHub.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Mqtts.Services;

/// <summary>
/// MQTT连接服务，负责处理设备连接认证、连接状态管理及相关事件发布
/// 实现了单例依赖注入，确保服务全局唯一
/// </summary>
[ExposeServices(typeof(IMqttConnectionService), typeof(IMqttService))]
public class MqttConnectionService : MqttServiceBase, IMqttConnectionService, ISingletonDependency
{
    #region 字段与常量定义

    /// <summary>
    /// 每个设备独立的熔断策略存储（线程安全集合）
    /// 键：设备唯一标识 ThingIdentifier
    /// 值：该设备的熔断策略
    /// </summary>
    private readonly ConcurrentDictionary<string, AsyncCircuitBreakerPolicy> _deviceCircuitBreakers = new();

    /// <summary>
    /// 跟踪已连接的客户端 (设备唯一标识 -> 连接状态)(线程安全集合)
    /// 用于拒绝多个设备使用相同信息（如：ProductKey、DeviceName）连接
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _connectedClients = new();

    /// <summary>
    /// 客户端ID与设备标识+UserName的映射
    /// 解决断开连接时无法获取UserName的问题
    /// </summary>
    private readonly ConcurrentDictionary<string, (string ThingIdentifier, string UserName)> _clientIdToDeviceMap = new();

    /// <summary>
    /// 熔断策略配置 - 允许失败的次数
    /// 当设备认证失败次数达到此值时，触发熔断
    /// </summary>
    private const int ExceptionsAllowedBeforeBreaking = 10;

    /// <summary>
    /// 熔断策略配置 - 熔断持续时间(秒)
    /// 熔断状态持续的时间，期间将拒绝该设备的连接请求
    /// </summary>
    private const int CircuitBreakDurationSeconds = 60;

    #endregion

    #region 依赖注入服务

    /// <summary>
    /// 日志服务
    /// </summary>
    protected ILogger<MqttConnectionService> Logger { get; }

    /// <summary>
    /// GUID生成器
    /// </summary>
    protected IGuidGenerator GuidGenerator { get; }

    protected DeviceMqttAuthManager DeviceMqttAuthManager { get; }


    protected IThingIdentifierGenerator ThingIdentifierGenerator { get; }

    /// <summary>
    /// 本地化字符串服务
    /// </summary>
    protected IStringLocalizer<IoTHubResource> Localizer { get; }


    /// <summary>
    /// 分布式事件总线
    /// </summary>
    protected IDistributedEventBus DistributedEventBus { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化MQTT连接服务
    /// </summary>
    /// <param name="logger">日志服务</param>
    /// <param name="guidGenerator">GUID生成器</param>
    /// <param name="distributedEventBus">分布式事件总线</param>
    /// <param name="localizer">本地化字符串服务</param>
    public MqttConnectionService(
        ILogger<MqttConnectionService> logger,
        IGuidGenerator guidGenerator,
        IStringLocalizer<IoTHubResource> localizer,
        DeviceMqttAuthManager deviceMqttAuthManager,
        IThingIdentifierGenerator thingIdentifierGenerator,
        IDistributedEventBus distributedEventBus)
        : base()
    {
        Logger = logger;
        GuidGenerator = guidGenerator;
        Localizer = localizer;
        DeviceMqttAuthManager = deviceMqttAuthManager;
        ThingIdentifierGenerator = thingIdentifierGenerator;
        DistributedEventBus = distributedEventBus;
    }

    #endregion

    #region MQTT服务器配置

    /// <summary>
    /// 配置MQTT服务器事件处理程序
    /// 注册连接验证、连接成功、断开连接事件
    /// </summary>
    /// <param name="mqttServer">MQTT服务器实例</param>
    public override void ConfigureMqttServer(MqttServer mqttServer)
    {
        base.ConfigureMqttServer(mqttServer);

        // 注册连接验证事件
        MqttServer.ValidatingConnectionAsync += ValidatingConnectionHandlerAsync;
        // 注册客户端连接成功事件
        MqttServer.ClientConnectedAsync += ClientConnectedHandlerAsync;
        // 注册客户端断开连接事件
        MqttServer.ClientDisconnectedAsync += ClientDisconnectedHandlerAsync;
    }

    #endregion

    #region 连接验证处理

    /// <summary>
    /// 连接验证处理程序
    /// 负责验证客户端连接请求的合法性，包括参数校验、重复连接检查和认证
    /// 使用Polly熔断机制防止恶意攻击
    /// </summary>
    /// <param name="eventArgs">连接验证事件参数</param>
    protected virtual async Task ValidatingConnectionHandlerAsync(ValidatingConnectionEventArgs eventArgs)
    {
        // 生成唯一追踪ID：便于全链路日志追踪
        var trackId = GuidGenerator.Create().ToString();

        try
        {
            Logger.LogInformation($"MQTT Server validating connection, clientId: {eventArgs.ClientId}, UserName: {eventArgs.UserName}");

            //// 1. 基础参数校验
            //if (!await ValidateBasicParametersAsync(eventArgs, trackId))
            //{
            //    return;
            //}

            #region TODO: 移动到领域服务，连接管理
            // 初步设想：将以下逻辑移到领域服务 MqttDeviceConnectionManager 中，
            // 这样可以更好地封装连接管理逻辑，提升代码的可维护性和可测试性。
            // 领域服务可以负责:
            // - 处理连接请求的验证、
            // - 重复连接检查
            // - 认证逻辑，

            //// 2. 解析认证参数
            var (authParams, parseSuccess) = await ParseAuthParametersAsync(eventArgs, trackId);
            if (!parseSuccess)
            {
                return;
            }

            // 设备唯一标识
            var thingIdentifier = ThingIdentifierGenerator.Generate(authParams.ProductKey, authParams.DeviceName);

            // 3. 重复连接校验
            if (!await ValidateDuplicateConnectionAsync(eventArgs, thingIdentifier, trackId))
            {
                return;
            }

            #endregion

            // 4. 执行带熔断保护的认证逻辑
            await ExecuteAuthenticationWithCircuitBreakerAsync(eventArgs, authParams, thingIdentifier, trackId);
        }
        catch (Exception ex)
        {
            // 处理认证过程中的未捕获异常
            eventArgs.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
            eventArgs.ReasonString = "服务器内部错误";
            Logger.LogError(ex, $"[{trackId}][MQTT 连接认证] 认证发生异常, ClientId: {eventArgs.ClientId}");
        }

        await Task.CompletedTask;
    }

    ///// <summary>
    ///// 验证基础连接参数
    ///// 检查ClientId、UserName和Password是否为空
    ///// </summary>
    ///// <param name="eventArgs">连接验证事件参数</param>
    ///// <returns>验证是否通过</returns>
    //private async Task<bool> ValidateBasicParametersAsync(ValidatingConnectionEventArgs eventArgs, string trackId)
    //{
    //    if (string.IsNullOrWhiteSpace(eventArgs.ClientId))
    //    {
    //        var ReasonString = "ClientId不能为空";
    //        eventArgs.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
    //        eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[{trackId}][MQTT 连接认证] 认证失败 | 原因：{Reason}}", trackId, ReasonString);

    //        return false;
    //    }

    //    if (string.IsNullOrWhiteSpace(eventArgs.UserName))
    //    {
    //        var ReasonString = "UserName不能为空";
    //        eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
    //        eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[{trackId}][MQTT 连接认证] 认证失败 | 原因：{Reason}}", trackId, ReasonString);

    //        return false;
    //    }

    //    if (eventArgs.Password == null)
    //    {
    //        var ReasonString = "Password不能为空";
    //        eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
    //        eventArgs.ReasonString = ReasonString;
    //        Logger.LogInformation("[{trackId}][MQTT 连接认证] 认证失败 | 原因：{Reason}}", trackId, ReasonString);

    //        return false;
    //    }

    //    return await Task.FromResult(true);
    //}

    /// <summary>
    /// 解析MQTT 签名参数
    /// 从ClientId和UserName中解析出ProductKey、DeviceName和认证类型等信息
    /// </summary>
    /// <param name="eventArgs">连接验证事件参数</param>
    /// <param name="trackId">追踪ID</param>
    /// <returns>解析得到的认证参数和解析是否成功的标志</returns>
    private async Task<(MqttSignParams? AuthParams, bool Success)> ParseAuthParametersAsync(ValidatingConnectionEventArgs eventArgs, string trackId)
    {
        // TODO?: 提取到领域服务 MqttDeviceAuthManager 中，
        var signParams = MqttSignHelper.ParseMqttClientIdAndUserName(eventArgs.ClientId, eventArgs.UserName);

        // 防御性校验：避免空指针/非法参数导致后续逻辑异常
        if (signParams == null || string.IsNullOrWhiteSpace(signParams.ProductKey) || string.IsNullOrWhiteSpace(signParams.DeviceName))
        {
            var ReasonString = "无法解析认证参数，请检查「ClientId」和「UserName」格式是否正确";
            eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            eventArgs.ReasonString = ReasonString;
            Logger.LogWarning("[{trackId}][MQTT 连接认证] 认证失败 | 无法解析认证参数 | ClientId={ClientId} | UserName={UserName} | 原因：{Reason}",
                trackId, eventArgs.ClientId, eventArgs.UserName, ReasonString);

            return (null, false);
        }

        return (signParams, true);
    }

    /// <summary>
    /// 验证重复连接
    /// 检查是否已有相同设备标识的客户端连接
    /// </summary>
    /// <param name="eventArgs">连接验证事件参数</param>
    /// <param name="thingIdentifier">设备唯一标识</param>
    /// <returns>验证是否通过</returns>
    private async Task<bool> ValidateDuplicateConnectionAsync(ValidatingConnectionEventArgs eventArgs, string thingIdentifier, string trackId)
    {
        if (_connectedClients.ContainsKey(thingIdentifier))
        {
            var ReasonString = "已存在相同设备的连接，请先断开现有连接";
            eventArgs.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            eventArgs.ReasonString = ReasonString;
            Logger.LogWarning("[{trackId}][MQTT 连接认证] 拒绝重复连接 | ClientId={ClientId} | UserName={UserName} | 原因：{Reason}",
                trackId, eventArgs.ClientId, eventArgs.UserName, ReasonString);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行带熔断保护的认证逻辑
    /// 使用设备专属的熔断策略执行认证，并处理认证结果
    /// </summary>
    /// <param name="eventArgs">连接验证事件参数</param>
    /// <param name="authParams">认证参数</param>
    /// <param name="thingIdentifier">设备唯一标识</param>
    private async Task ExecuteAuthenticationWithCircuitBreakerAsync(ValidatingConnectionEventArgs eventArgs, MqttSignParams authParams, string thingIdentifier, string trackId)
    {
        // 获取或创建设备专属的熔断策略
        var circuitBreaker = _deviceCircuitBreakers.GetOrAdd(thingIdentifier, CreateCircuitBreakerPolicy);

        try
        {
            // 执行带熔断保护的认证逻辑
            var authResult = await circuitBreaker.ExecuteAsync(async () =>
            {
                //return await HandleAuthenticationAsync(authParams, eventArgs, trackId);
                return await DeviceMqttAuthManager.ValidateMqttSignAsync(eventArgs.ClientId, eventArgs.UserName, eventArgs.Password);
            });

            // 处理认证结果
            if (!authResult.Succeeded)
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                eventArgs.ReasonString = authResult.ToString();
                Logger.LogWarning("[{trackId}][MQTT 连接认证] 认证失败 | ClientId={ClientId} | UserName={UserName} | 原因：{Reason}",
                   trackId, eventArgs.ClientId, eventArgs.UserName, authResult.ToString());

                return;
            }

            // 认证成功 - 提前存储设备标识与ClientId的映射
            _clientIdToDeviceMap.TryAdd(eventArgs.ClientId, (thingIdentifier, eventArgs.UserName));
            // 可以在这里缓存认证通过的设备信息，用于后续连接事件
            eventArgs.SessionItems[signParamsSessionItemKey] = authResult.Data;
            // 认证成功
            eventArgs.ReasonCode = MqttConnectReasonCode.Success;

            Logger.LogInformation("[{trackId}][MQTT 连接认证] 认证成功 | ClientId={ClientId} | UserName={UserName}",
                trackId, eventArgs.ClientId, eventArgs.UserName);
        }
        catch (BrokenCircuitException)
        {
            // 熔断状态处理
            eventArgs.ReasonCode = MqttConnectReasonCode.NotAuthorized;
            eventArgs.ReasonString = $"认证失败次数过多，已临时禁止连接，请{CircuitBreakDurationSeconds / 60}分钟后重试";

            var reason = $"设备[{thingIdentifier}]处于熔断状态，拒绝连接请求（熔断时长: {CircuitBreakDurationSeconds}秒）";
            Logger.LogWarning("[{trackId}][MQTT 连接认证] 认证失败 | 设备连接熔断中 | ClientId={ClientId} | UserName={UserName} | 原因：{Reason}",
                 trackId, eventArgs.ClientId, eventArgs.UserName, reason);
        }
    }

    #endregion

    #region 熔断策略管理

    /// <summary>
    /// 创建设备专属的熔断策略
    /// 防止恶意攻击导致的频繁认证失败
    /// </summary>
    /// <param name="deviceName">设备唯一标识</param>
    /// <returns>异步熔断策略</returns>
    private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(string deviceName)
    {
        return Policy
            .Handle<Exception>() // 捕获认证失败异常
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: ExceptionsAllowedBeforeBreaking,    // 失败N次后熔断
                durationOfBreak: TimeSpan.FromSeconds(CircuitBreakDurationSeconds),  // 熔断持续时间
                onBreak: (ex, breakDuration) =>
                {
                    Logger.LogWarning($"[MQTT 连接认证] 触发熔断 | 设备[{deviceName}]触发熔断，将在{breakDuration.TotalMinutes:F0}分钟内拒绝连接，原因: {ex.Message}");
                },
                onReset: () =>
                {
                    Logger.LogInformation($"[MQTT 连接认证] 重置熔断 | 设备[{deviceName}]熔断状态已重置，允许重新尝试连接");
                },
                onHalfOpen: () =>
                {
                    Logger.LogInformation($"[MQTT 连接认证] 半开熔断| 设备[{deviceName}]进入半开状态，允许尝试连接");
                }
            );
    }

    #endregion

    #region 连接状态事件处理

    /// <summary>
    /// 客户端连接成功事件处理程序
    /// 更新连接状态记录，并发布连接成功事件
    /// </summary>
    /// <param name="eventArgs">客户端连接事件参数</param>
    protected virtual async Task ClientConnectedHandlerAsync(ClientConnectedEventArgs eventArgs)
    {
        // 生成唯一追踪ID：便于全链路日志追踪
        var trackId = GuidGenerator.Create().ToString();
        var clientId = eventArgs.ClientId;

        try
        {
            var userName = eventArgs.UserName;
            var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

            // 防御性校验：避免空指针/非法参数导致后续逻辑异常
            if (authParams == null || string.IsNullOrWhiteSpace(authParams.ProductKey) || string.IsNullOrWhiteSpace(authParams.DeviceName))
            {
                Logger.LogWarning($"[{trackId}][MQTT 连接认证] 客户端[{clientId}]连接成功，但解析认证参数失败，UserName: {userName}");
                return;
            }

            var thingIdentifier = ThingIdentifierGenerator.Generate(authParams.ProductKey, authParams.DeviceName);
            _connectedClients.TryAdd(thingIdentifier, true); // 添加客户端连接记录

            // 构建连接事件对象
            var eto = new MqttClientConnectedEto
            {
                MqttTrackId = trackId,
                MqttClientId = eventArgs.ClientId,
                ProductKey = authParams?.ProductKey,
                DeviceName = authParams?.DeviceName,
                ProtocolVersion = eventArgs.ProtocolVersion,
                Endpoint = eventArgs.RemoteEndPoint.ToString()
            };

            // 发布分布式事件（带ConfigureAwait(false)避免上下文切换，指定参数保证可靠性）
            await DistributedEventBus.PublishAsync(eto, onUnitOfWorkComplete: false, useOutbox: true).ConfigureAwait(false);

            Logger.LogInformation($"[{trackId}][MQTT 连接认证] 设备连接成功，设备：{authParams.ProductKey}/{authParams.DeviceName}, MqttClientId: {clientId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"[{trackId}][MQTT 连接认证] 处理客户端[{clientId}]连接成功事件时发生异常");
            // 异常兜底：清理可能残留的连接记录，避免设备状态不一致
            await CleanupConnectionRecordsAsync(clientId, eventArgs.UserName, trackId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 客户端断开连接事件处理程序
    /// 更新连接状态记录，并发布断开连接事件
    /// </summary>
    /// <param name="eventArgs">客户端断开连接事件参数</param>
    protected virtual async Task ClientDisconnectedHandlerAsync(ClientDisconnectedEventArgs eventArgs)
    {
        // 生成唯一追踪ID：便于全链路日志追踪
        var trackId = GuidGenerator.Create().ToString();
        var clientId = eventArgs.ClientId;

        try
        {
            // 移除客户端连接记录
            if (_clientIdToDeviceMap.TryRemove(clientId, out var deviceMap))
            {
                var thingIdentifier = deviceMap.ThingIdentifier;
                var userName = deviceMap.UserName;

                // 从连接列表中移除设备
                _connectedClients.TryRemove(thingIdentifier, out _);

                //从会话项中获取认证参数,不主动删除，让 MQTT 服务器 .Session 管理机制处理。
                //eventArgs.SessionItems.Remove(AuthParamsSessionItemKey);

                // 解析认证参数用于事件发布
                var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

                // 构建断开连接事件对象
                var eto = new MqttClientDisconnectedEto
                {
                    MqttTrackId = trackId,
                    MqttClientId = clientId,
                    ProductKey = authParams?.ProductKey,
                    DeviceName = authParams?.DeviceName,
                    Endpoint = eventArgs.Endpoint
                };

                await DistributedEventBus.PublishAsync(eto, onUnitOfWorkComplete: false, useOutbox: true).ConfigureAwait(false);

                Logger.LogInformation($"[{trackId}][MQTT 连接认证] 设备断开连接，设备：{authParams?.ProductKey}/{authParams?.DeviceName}, MqttClientId: {eventArgs.ClientId}");
            }
            else
            {
                Logger.LogWarning($"[{trackId}][MQTT 连接认证] 未找到客户端[{clientId}]对应的设备标识，无法清理连接记录");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"[{trackId}][MQTT 连接认证] 断开连接异常| 处理客户端[{eventArgs.ClientId}]断开连接事件时发生异常");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 清理连接记录
    /// 当连接事件处理发生异常时，确保连接状态记录被正确清理
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="trackId">追踪ID</param>
    private async Task CleanupConnectionRecordsAsync(string clientId, string userName, string trackId)
    {
        var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);
        if (authParams != null && !string.IsNullOrWhiteSpace(authParams.ProductKey) && !string.IsNullOrWhiteSpace(authParams.DeviceName))
        {
            var deviceName = $"{authParams.ProductKey}:{authParams.DeviceName}";
            _connectedClients.TryRemove(deviceName, out _);
            _clientIdToDeviceMap.TryRemove(clientId, out _);
            Logger.LogWarning($"[{trackId}][MQTT 连接认证] 客户端[{clientId}]连接事件异常，已兜底清理设备[{deviceName}]连接记录");
        }

        await Task.CompletedTask;
    }

    #endregion
}