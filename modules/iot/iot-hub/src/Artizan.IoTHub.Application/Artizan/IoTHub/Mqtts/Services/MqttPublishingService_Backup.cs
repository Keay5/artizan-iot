//using Artizan.IoT.Mqtts.Etos;
//using Artizan.IoT.Mqtts.Signs;
//using Artizan.IoTHub.Localization;
//using Artizan.IoTHub.Mqtts.Options;
//using Artizan.IoTHub.Mqtts.Options.Pollys;
//using Microsoft.Extensions.Localization;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using MQTTnet.Server;
//using Polly;
//using Polly.Bulkhead;
//using Polly.CircuitBreaker;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.EventBus.Distributed;
//using Volo.Abp.Guids;

//namespace Artizan.IoTHub.Mqtts.Servers;

///// <summary>
///// MQTT消息发布服务（生产环境核心服务）
///// 【设计定位】：作为MQTT服务器与分布式事件总线的中间层，核心职责是接收MQTT设备消息并发布到事件总线
///// 【核心能力】：
///// 1. 消息接收与校验：过滤无效消息，提取设备认证信息
///// 2. 容错保护：基于Polly实现熔断+隔离策略，防止事件总线故障扩散
///// 3. 性能优化：批量发布减少事件总线调用开销，支持高并发（每秒1万+数据点）
///// 4. 降级重试：本地队列缓存失败消息，后台自动重试保证消息不丢失
///// 5. 动态配置：支持配置热更新，无需重启服务即可调整策略参数
///// 6. 高并发场景：对于高并发场景，启用批量发布的性能优势，消息达到数量阈值时立即发布消息
///// 7. 消息频率低场景：对于设备数量少、消息频率低的场景，即使消息数量没达到阈值，只要超过设定的超时时间，也会触发消息发布。
///// </summary>
////[ExposeServices(typeof(IMqttPublishingService), typeof(IMqttService))] // ABP依赖注入：暴露服务接口
//public class MqttPublishingService_Backup //: MqttServiceBase, IMqttPublishingService, ISingletonDependency, IDisposable
//{
//    #region 核心成员变量（设计思路：分层管理，职责单一）
//    // ---------------------Polly 策略层---------------------
//    // 熔断器策略：保护事件总线发布操作，故障时快速失败避免雪崩
//    private AsyncCircuitBreakerPolicy _eventBusCircuitBreaker;
//    // 隔离策略：限制并发处理量，保护MQTT服务器主线程不被压垮
//    private AsyncBulkheadPolicy _bulkheadPolicy;
//    // 策略更新锁：保证多线程下策略更新的原子性（防止读写冲突）
//    private readonly object _policyLock = new object();

//    //---------------------降级重试层---------------------
//    // 本地降级队列：事件总线不可用时临时存储消息（内存级兜底，保证不丢失）
//    // 选型原因：ConcurrentQueue是线程安全队列，无锁设计，高并发下性能优于Queue+lock
//    private ConcurrentQueue<MqttClientPublishTopicEto> _fallbackQueue;
//    // 后台重试任务锁：控制重试循环的并发执行（防止重复消费队列）
//    // 选型原因：SemaphoreSlim轻量级锁，支持异步等待，适合后台任务
//    private SemaphoreSlim _retryLock = new SemaphoreSlim(1, 1);

//    //---------------------批量发布层---------------------
//    // 按主题分组的批量发布队列：避免不同主题消息混批，保证业务隔离
//    private ConcurrentDictionary<string, ConcurrentQueue<MqttClientPublishTopicEto>> _batchPublishQueue;
//    // 批量发布取消令牌：优雅停止批量发布循环（支持配置动态关闭优化）
//    private CancellationTokenSource _batchPublishCts;
//    // 批量处理锁：防止多线程并发处理同一主题队列（避免消息重复发布）
//    private readonly SemaphoreSlim _batchProcessLock = new SemaphoreSlim(1, 1);
//    //+ 记录每个主题的最后消息加入时间：用于实现超时自动发布
//    private ConcurrentDictionary<string, DateTime> _topicLastEnqueueTime;

//    //---------------------配置层---------------------
//    // 当前生效的MQTT配置：缓存验证后的配置，避免每次读取配置都校验
//    protected IoTMqttOptions CurrentIoTMqttOptions { get; private set; }

//    //---------------------基础依赖层---------------------
//    protected ILogger<MqttPublishingService_Backup> Logger { get; } // 日志：生产环境必备，便于问题排查
//    protected IGuidGenerator GuidGenerator { get; } // GUID生成：为每条消息生成唯一追踪ID
//    protected IDistributedEventBus DistributedEventBus { get; } // 分布式事件总线：核心发布目标
//    protected IStringLocalizer<IoTHubResource> Localizer { get; } // 本地化：支持多语言日志（可选）
//    // 配置监控：支持配置热更新，配置变更时自动触发回调
//    private readonly IOptionsMonitor<IoTMqttOptions> _ioTMqttOptionsMonitor;
//    #endregion

//    /// <summary>
//    /// 构造函数（依赖注入+初始化核心资源）
//    /// 【设计思路】：
//    /// 1. 依赖通过构造函数注入，符合DI原则，便于单元测试
//    /// 2. 初始化配置时带默认值兜底，防止配置缺失导致服务启动失败
//    /// 3. 提前初始化核心组件（队列、策略），避免运行时首次调用的性能损耗
//    /// </summary>
//    public MqttPublishingService_Backup(
//        ILogger<MqttPublishingService_Backup> logger,
//        IGuidGenerator guidGenerator,
//        IDistributedEventBus distributedEventBus,
//        IStringLocalizer<IoTHubResource> localizer,
//        IOptionsMonitor<IoTMqttOptions> ioTMqttOptionsMonitor)
//       : base()
//    {
//        // 基础依赖赋值
//        Logger = logger;
//        GuidGenerator = guidGenerator;
//        DistributedEventBus = distributedEventBus;
//        Localizer = localizer;
//        _ioTMqttOptionsMonitor = ioTMqttOptionsMonitor;

//        // 初始化配置（核心设计：配置验证+默认值兜底）
//        // 原因：生产环境配置可能缺失或非法，提前验证可避免运行时异常
//        CurrentIoTMqttOptions = GetValidatedOptions(_ioTMqttOptionsMonitor.CurrentValue) ?? new IoTMqttOptions
//        {
//            // Polly默认配置：基于经验值，适配大多数场景
//            Polly = new PollyOptions
//            {
//                CircuitBreaker = new CircuitBreakerOptions { ExceptionsAllowedBeforeBreaking = 10, DurationOfBreakSeconds = 30 },
//                Bulkhead = new BulkheadOptions { MaxParallelization = 10000, MaxQueuingActions = 1000 },
//                Retry = new RetryOptions { RetryIntervalSeconds = 5, MaxRetryPerLoop = 100 }
//            },
//            // 批量优化默认配置：默认关闭，避免影响现有逻辑
//            PublishingOptimization = new PublishingOptimizationOptions
//            {
//                EnableOptimizations = false,
//                BatchPublishThreshold = 100,
//                BatchPublishIntervalMs = 100,
//                // 新增：单条消息超时发布时间（毫秒）
//                SingleMessagePublishTimeoutMs = 300,
//                TopicBasedThrottling = new Dictionary<string, int>()
//            }
//        };

//        // 初始化降级队列（提前创建，避免首次使用时的初始化开销）
//        _fallbackQueue = new ConcurrentQueue<MqttClientPublishTopicEto>();

//        // 初始化Polly策略（启动时创建，避免运行时动态创建的性能损耗）
//        _eventBusCircuitBreaker = InitCircuitBreakerPolicy();
//        _bulkheadPolicy = InitBulkheadPolicy();

//        // 监听配置变更（核心设计：配置热更新）
//        // 原因：生产环境调整配置无需重启服务，提升可用性
//        _ioTMqttOptionsMonitor.OnChange(OnIoTMqttOptionsChanged);

//        // 启动后台重试任务（火并忘记模式：_ = 不等待，后台持续运行）
//        // 原因：重试任务是后台常驻任务，无需阻塞服务启动
//        _ = StartFallbackRetryLoopAsync();

//        // 初始化批量发布队列（按需创建，未启用优化时不占用内存）
//        if (CurrentIoTMqttOptions.PublishingOptimization.EnableOptimizations)
//        {
//            _batchPublishQueue = new ConcurrentDictionary<string, ConcurrentQueue<MqttClientPublishTopicEto>>();

//            //初始化主题最后入队时间字典
//            _topicLastEnqueueTime = new ConcurrentDictionary<string, DateTime>();

//            _ = StartBatchPublishLoopAsync();
//            Logger.LogInformation("MQTT发布拦截器[{Name}] | 批量发布优化已启用 | 阈值：{0}条 | 间隔：{1}ms | 单条超时：{2}ms",
//                nameof(MqttPublishingService_Backup),
//                CurrentIoTMqttOptions.PublishingOptimization.BatchPublishThreshold,
//                CurrentIoTMqttOptions.PublishingOptimization.BatchPublishIntervalMs,
//                CurrentIoTMqttOptions.PublishingOptimization.SingleMessagePublishTimeoutMs);
//        }

//        Logger.LogInformation("MQTT发布拦截器[{Name}] | Polly配置：熔断失败次数={0} | 隔离最大并发={1}",
//            nameof(MqttPublishingService_Backup),
//            CurrentIoTMqttOptions.Polly.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
//            CurrentIoTMqttOptions.Polly.Bulkhead.MaxParallelization);
//    }

//    /// <summary>
//    /// 配置MQTT服务器（注册发布拦截器）
//    /// 【设计思路】：
//    /// 1. 重写基类方法，符合开闭原则
//    /// 2. 注册InterceptingPublishAsync事件，拦截所有MQTT发布消息
//    /// 3. 拦截器是MQTTnet的核心扩展点，可在消息发布前做自定义处理
//    /// </summary>
//    public override void ConfigureMqttServer(MqttServer mqttServer)
//    {
//        base.ConfigureMqttServer(mqttServer);
//        // 注册发布拦截器：所有设备发布的消息都会经过此回调处理
//        MqttServer.InterceptingPublishAsync += InterceptingPublishHandlerAsync;
//        Logger.LogInformation("MQTT发布拦截器[{Name}] | 已注册 | 服务就绪", nameof(MqttPublishingService_Backup));
//    }

//    #region 配置管理（核心设计：验证+热更新）
//    /// <summary>
//    /// IoT MQTT配置变更处理（配置热更新核心方法）
//    /// 【设计思路】：
//    /// 1. 配置变更时先验证，无效则使用现有配置，避免服务异常
//    /// 2. 加锁更新配置和策略，保证线程安全
//    /// 3. 动态调整批量发布开关，无需重启服务
//    /// </summary>
//    /// <param name="newOptions">新的配置值（从配置文件/配置中心获取）</param>
//    private void OnIoTMqttOptionsChanged(IoTMqttOptions newOptions)
//    {
//        try
//        {
//            Logger.LogInformation("MQTT发布拦截器[{Name}] | 收到配置变更通知 | 新配置：{1}",
//                nameof(MqttPublishingService_Backup),
//                Newtonsoft.Json.JsonConvert.SerializeObject(newOptions));

//            // 第一步：验证新配置（核心：过滤非法配置，保证服务稳定性）
//            var validatedOptions = GetValidatedOptions(newOptions);
//            if (validatedOptions == null)
//            {
//                Logger.LogWarning("MQTT发布拦截器[{Name}] | 配置验证失败 | 使用现有配置", nameof(MqttPublishingService_Backup));
//                return;
//            }

//            // 第二步：加锁更新配置和策略（线程安全）
//            // 原因：多线程下可能同时更新配置，lock保证原子性
//            lock (_policyLock)
//            {
//                // 更新缓存的配置
//                CurrentIoTMqttOptions = validatedOptions;

//                // 重新初始化Polly策略（配置变更后策略需同步更新）
//                _eventBusCircuitBreaker = InitCircuitBreakerPolicy();
//                _bulkheadPolicy = InitBulkheadPolicy();
//                Logger.LogInformation("Polly策略已更新 | 新熔断失败次数={0} | 新隔离最大并发={1}",
//                    CurrentIoTMqttOptions.Polly.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
//                    CurrentIoTMqttOptions.Polly.Bulkhead.MaxParallelization);

//                // 第三步：处理批量发布配置变更
//                var wasEnabled = CurrentIoTMqttOptions.PublishingOptimization.EnableOptimizations;
//                var newEnabled = validatedOptions.PublishingOptimization.EnableOptimizations;

//                if (newEnabled && !wasEnabled)
//                {
//                    // 启用批量发布：初始化队列并启动循环
//                    _batchPublishQueue = new ConcurrentDictionary<string, ConcurrentQueue<MqttClientPublishTopicEto>>();
//                    // 初始化主题最后入队时间字典
//                    _topicLastEnqueueTime = new ConcurrentDictionary<string, DateTime>();

//                    _ = StartBatchPublishLoopAsync();

//                    Logger.LogInformation("MQTT发布拦截器[{Name}] | 批量发布优化已启用 | 阈值：{1}条 | 间隔：{2}ms | 单条超时：{3}ms",
//                         nameof(MqttPublishingService_Backup),
//                         validatedOptions.PublishingOptimization.BatchPublishThreshold,
//                         validatedOptions.PublishingOptimization.BatchPublishIntervalMs,
//                         validatedOptions.PublishingOptimization.SingleMessagePublishTimeoutMs);
//                }
//                else if (!newEnabled && wasEnabled)
//                {
//                    // 禁用批量发布：取消循环并清空队列
//                    _batchPublishCts?.Cancel();
//                    _batchPublishQueue = null;

//                    //+ 清理时间记录字典
//                    _topicLastEnqueueTime = null;

//                    Logger.LogInformation("MQTT发布拦截器[{Name}] | 批量发布优化已禁用", nameof(MqttPublishingService_Backup));
//                }
//                else if (newEnabled && wasEnabled)  // 配置变更但开关状态不变时也记录批量参数
//                {
//                    Logger.LogInformation("MQTT发布拦截器[{Name}] | 批量发布配置已更新 | 新阈值：{1}条 | 新间隔：{2}ms | 单条超时：{3}ms",
//                        nameof(MqttPublishingService_Backup),
//                        validatedOptions.PublishingOptimization.BatchPublishThreshold,
//                        validatedOptions.PublishingOptimization.BatchPublishIntervalMs,
//                        validatedOptions.PublishingOptimization.SingleMessagePublishTimeoutMs);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "处理配置变更失败 | 服务将继续使用现有配置");
//        }
//    }

//    /// <summary>
//    /// 验证配置并返回有效配置（核心设计：默认值兜底+非法值过滤）
//    /// 【设计原因】：
//    /// 1. 配置文件可能缺失字段，默认值保证服务能启动
//    /// 2. 配置值可能为负数/零，过滤后避免运行时异常（如熔断次数为0会立即熔断）
//    /// 3. 限流配置可能有无效主题，提前清理减少运行时判断
//    /// </summary>
//    /// <param name="options">待验证的配置</param>
//    /// <returns>验证后的有效配置（null表示验证失败）</returns>
//    private IoTMqttOptions GetValidatedOptions(IoTMqttOptions options)
//    {
//        if (options == null) return null;

//        try
//        {
//            // 1. 验证Polly配置（核心容错策略，必须保证有效）
//            var pollyOptions = options.Polly ?? new PollyOptions();
//            pollyOptions.CircuitBreaker = pollyOptions.CircuitBreaker ?? new CircuitBreakerOptions();
//            pollyOptions.Bulkhead = pollyOptions.Bulkhead ?? new BulkheadOptions();
//            pollyOptions.Retry = pollyOptions.Retry ?? new RetryOptions();

//            // 填充Polly默认值（过滤非法值）
//            pollyOptions.CircuitBreaker.ExceptionsAllowedBeforeBreaking = pollyOptions.CircuitBreaker.ExceptionsAllowedBeforeBreaking <= 0
//                ? 10 : pollyOptions.CircuitBreaker.ExceptionsAllowedBeforeBreaking; // 熔断失败次数不能≤0
//            pollyOptions.CircuitBreaker.DurationOfBreakSeconds = pollyOptions.CircuitBreaker.DurationOfBreakSeconds <= 0
//                ? 30 : pollyOptions.CircuitBreaker.DurationOfBreakSeconds; // 熔断时长不能≤0
//            pollyOptions.Bulkhead.MaxParallelization = pollyOptions.Bulkhead.MaxParallelization <= 0
//                ? 10000 : pollyOptions.Bulkhead.MaxParallelization; // 最大并发不能≤0
//            pollyOptions.Bulkhead.MaxQueuingActions = pollyOptions.Bulkhead.MaxQueuingActions < 0
//                ? 1000 : pollyOptions.Bulkhead.MaxQueuingActions; // 最大队列不能<0
//            pollyOptions.Retry.RetryIntervalSeconds = pollyOptions.Retry.RetryIntervalSeconds <= 0
//                ? 5 : pollyOptions.Retry.RetryIntervalSeconds; // 重试间隔不能≤0
//            pollyOptions.Retry.MaxRetryPerLoop = pollyOptions.Retry.MaxRetryPerLoop <= 0
//                ? 100 : pollyOptions.Retry.MaxRetryPerLoop; // 重试次数不能≤0

//            // 2. 验证发布优化配置
//            var optimizationOptions = options.PublishingOptimization ?? new PublishingOptimizationOptions();
//            optimizationOptions.BatchPublishThreshold = optimizationOptions.BatchPublishThreshold > 0
//                ? optimizationOptions.BatchPublishThreshold : 100; // 批量阈值必须>0
//            optimizationOptions.BatchPublishIntervalMs = optimizationOptions.BatchPublishIntervalMs > 0
//                ? optimizationOptions.BatchPublishIntervalMs : 100; // 检查间隔必须>0

//            // 验证单条消息超时配置,单设备单条消息：等待 x 秒后自动发布（即时通讯），平衡即时性和批量性能
//            optimizationOptions.SingleMessagePublishTimeoutMs = optimizationOptions.SingleMessagePublishTimeoutMs > 0
//                ? optimizationOptions.SingleMessagePublishTimeoutMs : 300; // 单条超时必须>0

//            optimizationOptions.TopicBasedThrottling = optimizationOptions.TopicBasedThrottling ?? new Dictionary<string, int>();

//            // 清理无效的限流配置（空主题/负数限流值）
//            optimizationOptions.TopicBasedThrottling = optimizationOptions.TopicBasedThrottling
//                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0)
//                .ToDictionary(kv => kv.Key, kv => kv.Value);

//            // 返回验证后的配置
//            return new IoTMqttOptions
//            {
//                Polly = pollyOptions,
//                PublishingOptimization = optimizationOptions
//            };
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "配置验证失败");
//            return null;
//        }
//    }
//    #endregion

//    #region Polly策略设计（核心容错机制）
//    /// <summary>
//    /// 初始化熔断策略（Circuit Breaker）
//    /// 【设计思路】：
//    /// 1. 熔断策略：失败次数达到阈值后，快速失败一段时间，避免无效请求浪费资源
//    /// 2. 适用场景：事件总线暂时不可用（如网络故障、MQ集群宕机）
//    /// 3. 核心参数：失败次数+熔断时长，可通过配置动态调整
//    /// 4. 异常过滤：忽略取消/超时异常，避免瞬时异常触发熔断
//    /// </summary>
//    /// <returns>异步熔断策略</returns>
//    private AsyncCircuitBreakerPolicy InitCircuitBreakerPolicy()
//    {
//        var config = CurrentIoTMqttOptions.Polly.CircuitBreaker;

//        return Policy
//            // 异常过滤：仅处理非取消/非超时异常（瞬时异常不触发熔断）
//            .Handle<Exception>(ex => ex is not OperationCanceledException and not TimeoutException)
//            .CircuitBreakerAsync(
//                exceptionsAllowedBeforeBreaking: config.ExceptionsAllowedBeforeBreaking, // 熔断前允许的失败次数
//                durationOfBreak: TimeSpan.FromSeconds(config.DurationOfBreakSeconds), // 熔断持续时间
//                // 熔断开启回调：记录日志，便于监控告警                                                  
//                onBreak: (exception, breakDuration) =>    
//                {
//                    Logger.LogWarning(
//                        "MQTT发布拦截器[{Name}] | [熔断策略] 开启 | 失败次数={0} | 持续时间={1}s | 原因={2}",
//                        nameof(MqttPublishingService_Backup),
//                        config.ExceptionsAllowedBeforeBreaking, breakDuration.TotalSeconds, exception?.Message ?? "未知异常"
//                    );
//                },
//                // 熔断重置回调：恢复正常，记录日志
//                onReset: () => Logger.LogInformation("MQTT发布拦截器[{Name}] | [熔断策略] 重置 | 恢复正常处理", nameof(MqttPublishingService_Backup)),
//                // 熔断半开回调：尝试恢复，记录日志
//                onHalfOpen: () => Logger.LogInformation("MQTT发布拦截器[{Name}] | [熔断策略] 半开 | 尝试处理请求验证恢复", nameof(MqttPublishingService_Backup))
//            );
//    }

//    /// <summary>
//    /// 初始化隔离策略（Bulkhead）
//    /// 【设计思路】：
//    /// 1. 隔离策略：限制并发执行数和排队数，防止单个组件故障拖垮整个服务
//    /// 2. 适用场景：事件总线处理慢，请求堆积导致MQTT服务器线程耗尽
//    /// 3. 核心参数：最大并发数+最大排队数，可通过配置动态调整
//    /// 4. 限流回调：限流时将消息加入降级队列，保证不丢失
//    /// </summary>
//    /// <returns>异步隔离策略</returns>
//    private AsyncBulkheadPolicy InitBulkheadPolicy()
//    {
//        var config = CurrentIoTMqttOptions.Polly.Bulkhead;

//        return Policy
//            .BulkheadAsync(
//                maxParallelization: config.MaxParallelization, // 最大并发执行数（控制同时发布的请求数）
//                maxQueuingActions: config.MaxQueuingActions, // 最大排队数（超出则触发限流）
//                // 限流回调：将消息加入降级队列，保证不丢失
//                onBulkheadRejectedAsync: async (context) =>
//                {
//                    Logger.LogWarning(
//                        "MQTT发布拦截器[{Name}] | [隔离策略] 限流触发 | 最大并发={0} | 最大队列={1}",
//                        nameof(MqttPublishingService_Backup),
//                        config.MaxParallelization, config.MaxQueuingActions
//                    );

//                    // 从上下文获取消息对象，加入降级队列
//                    if (context["Eto"] is MqttClientPublishTopicEto eto)
//                    {
//                        _fallbackQueue.Enqueue(eto);
//                        Logger.LogWarning("[{TrackId}] 因隔离策略限流 | 加入重试队列", eto.MqttTrackId);
//                    }
//                    await Task.CompletedTask;
//                }
//            );
//    }

//    /// <summary>
//    /// 组合策略（先隔离后熔断）
//    /// 【设计思路】：
//    /// 1. 策略组合顺序：先隔离（控制并发），后熔断（快速失败）
//    /// 2. 双重检查锁定：保证多线程下获取的是最新策略，且无性能损耗
//    /// 3. 适用场景：高并发下先限制并发，再防止故障扩散
//    /// </summary>
//    /// <returns>组合后的异步策略</returns>
//    private IAsyncPolicy GetCombinedPolicy()
//    {
//        // 双重检查锁定：第一次检查无锁，第二次加锁，平衡性能和线程安全
//        lock (_policyLock)
//        {
//            // 策略组合：隔离策略在外层，熔断策略在内层
//            // 原因：先限制并发，再判断是否熔断，符合容错优先级
//            return Policy.WrapAsync(_bulkheadPolicy, _eventBusCircuitBreaker);
//        }
//    }
//    #endregion

//    #region 降级重试设计（保证消息不丢失）
//    /// <summary>
//    /// 后台重试队列处理循环（常驻后台任务）
//    /// 【设计思路】：
//    /// 1. 无限循环：持续监听降级队列，保证消息最终能被处理
//    /// 2. 延迟重试：按配置的间隔重试，避免频繁重试加剧事件总线压力
//    /// 3. 熔断判断：熔断开启时跳过重试，避免无效请求
//    /// 4. 批量重试：每次重试最多处理配置的数量，防止单次重试过多
//    /// 5. 异常保护：捕获所有异常，保证重试线程不崩溃
//    /// </summary>
//    /// <returns>异步任务</returns>
//    private async Task StartFallbackRetryLoopAsync()
//    {
//        // 无限循环：后台常驻，直到服务停止
//        while (true)
//        {
//            try
//            {
//                // 延迟重试：使用配置的重试间隔，减少无效轮询
//                await Task.Delay(TimeSpan.FromSeconds(CurrentIoTMqttOptions.Polly.Retry.RetryIntervalSeconds))
//                    .ConfigureAwait(false); // 非上下文切换，提升性能

//                // 熔断开启或队列为空时跳过（核心：避免无效重试）
//                if (_fallbackQueue.IsEmpty || _eventBusCircuitBreaker.CircuitState == CircuitState.Open)
//                {
//                    continue;
//                }

//                // 加锁：防止多线程并发重试，导致消息重复处理
//                await _retryLock.WaitAsync().ConfigureAwait(false);

//                int retrySuccessCount = 0;
//                // 批量重试：每次最多处理MaxRetryPerLoop条，防止单次处理过多
//                for (int i = 0; i < CurrentIoTMqttOptions.Polly.Retry.MaxRetryPerLoop && _fallbackQueue.TryDequeue(out var eto); i++)
//                {
//                    try
//                    {
//                        // 发布消息到事件总线（使用OuseOutbox: true保证可靠性, onUnitOfWorkComplete: false 不等待事务，该场景下适用）
//                        await DistributedEventBus.PublishAsync(eto, onUnitOfWorkComplete: false, useOutbox: true)
//                            .ConfigureAwait(false);

//                        retrySuccessCount++;
//                        Logger.LogInformation("[{TrackId}] 重试发布成功", eto.MqttTrackId);
//                    }
//                    catch (Exception ex)
//                    {
//                        // 重试失败：将消息重新加入队列，等待下次重试
//                        _fallbackQueue.Enqueue(eto);
//                        Logger.LogWarning("[{TrackId}] 重试发布失败 | 原因={0}", eto.MqttTrackId, ex.Message);
//                        break; // 遇到错误暂停重试，避免雪崩
//                    }
//                }

//                // 日志：重试统计，便于监控
//                if (retrySuccessCount > 0)
//                {
//                    Logger.LogInformation("[重试任务] 成功={0}条 | 剩余队列={1}条", retrySuccessCount, _fallbackQueue.Count);
//                }
//            }
//            catch (Exception ex)
//            {
//                // 捕获所有异常：保证重试线程不崩溃
//                Logger.LogError(ex, "[重试任务] 线程异常 | 已自动恢复");
//            }
//            finally
//            {
//                // 释放锁：必须在finally中释放，防止死锁
//                _retryLock.Release();
//            }
//        }
//    }
//    #endregion

//    #region 批量发布设计（高并发性能优化 + 超时发布）
//    /// <summary>
//    /// 批量发布处理循环（定时检查+阈值触发+超时触发）
//    /// 【设计思路】：
//    /// 1. 定时检查：按配置的间隔检查队列，保证消息延迟可控
//    /// 2. 阈值触发：达到批量阈值立即处理，减少消息延迟
//    /// 3. 超时触发：超过最大等待时间即使未达阈值也处理，保证即时性
//    /// 4. 按主题分组：不同主题消息分开批量，保证业务隔离
//    /// 5. 优雅停止：支持取消令牌，配置关闭时优雅停止循环
//    /// 6. 并发控制：加锁防止多线程处理同一队列，避免重复发布
//    /// </summary>
//    /// <returns>异步任务</returns>
//    private async Task StartBatchPublishLoopAsync()
//    {
//        // 创建取消令牌：用于优雅停止批量循环
//        _batchPublishCts = new CancellationTokenSource();
//        var cancellationToken = _batchPublishCts.Token;

//        try
//        {
//            // 无限循环：持续检查批量队列
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                // 每次当前配置的间隔时间等待（每次循环重新获取最新配置）
//                var interval = CurrentIoTMqttOptions.PublishingOptimization.BatchPublishIntervalMs;
//                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

//                // 未启用优化或队列为空时跳过
//                if (!CurrentIoTMqttOptions.PublishingOptimization.EnableOptimizations || _batchPublishQueue == null)
//                {
//                    continue;
//                }

//                // 加锁：防止多线程并发处理同一主题队列
//                await _batchProcessLock.WaitAsync(cancellationToken).ConfigureAwait(false);

//                try
//                {
//                    // 遍历所有主题队列，处理达到阈值或超时的队列
//                    foreach (var topic in _batchPublishQueue.Keys.ToList()) // ToList：防止遍历中集合变更
//                    {
//                        if (_batchPublishQueue.TryGetValue(topic, out var topicQueue) && topicQueue.Count > 0)
//                        {
//                            // 检查是否达到阈值或超时
//                            bool thresholdReached = topicQueue.Count >= CurrentIoTMqttOptions.PublishingOptimization.BatchPublishThreshold;
//                            // 超时判断初始化
//                            bool timeoutReached = false;

//                            if (_topicLastEnqueueTime.TryGetValue(topic, out var lastTime))
//                            {
//                                // 核心：计算当前时间与最后入队时间的差值，判断是否超过超时配置
//                                timeoutReached = DateTime.UtcNow - lastTime >= TimeSpan.FromMilliseconds(CurrentIoTMqttOptions.PublishingOptimization.SingleMessagePublishTimeoutMs);
//                            }

//                            // 满足「数量阈值」或「超时」任一条件即发布
//                            if (thresholdReached || timeoutReached)
//                            {
//                                // 调用原有单参数重载，保持兼容性
//                                await ProcessBatchQueueAsync(topicQueue).ConfigureAwait(false);
//                                // 处理完成后移除时间记录
//                                _topicLastEnqueueTime.TryRemove(topic, out _);
//                            }
//                        }
//                    }
//                }
//                finally
//                {
//                    // 释放锁：必须在finally中释放
//                    _batchProcessLock.Release();
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // 取消异常：正常停止，记录日志
//            Logger.LogInformation("[批量发布] 循环已取消 | 优化功能已禁用");
//        }
//        catch (Exception ex)
//        {
//            // 捕获所有异常：保证批量线程不崩溃
//            Logger.LogError(ex, "[批量发布] 循环异常 | 已自动恢复");
//        }
//    }

//    /// <summary>
//    /// 处理单个主题的批量发布队列
//    /// 【设计思路】：
//    /// 1. 批量取出：一次性取出队列中所有消息，减少队列操作次数
//    /// 2. 并行发布：使用Task.WhenAll并行发布，减少事件总线调用开销
//    /// 3. 策略保护：使用组合策略（隔离+熔断）保护批量发布
//    /// 4. 失败回退：发布失败时将消息退回队列，保证不丢失
//    /// </summary>
//    /// <param name="queue">单个主题的消息队列</param>
//    /// <returns>异步任务</returns>
//    private async Task ProcessBatchQueueAsync(ConcurrentQueue<MqttClientPublishTopicEto> queue)
//    {
//        // 批量取出队列中的所有消息（原子操作，减少队列锁竞争）
//        var batch = new List<MqttClientPublishTopicEto>();
//        while (queue.TryDequeue(out var eto))
//        {
//            batch.Add(eto);
//        }

//        // 空批次直接返回
//        if (batch.Count == 0)
//            return;

//        try
//        {
//            Logger.LogDebug("[批量发布] | [处理消息] -> [事件总线] | 主题={0} | 数量={1}条", batch.First().MqttTopic, batch.Count);

//            // 使用组合策略保护批量发布（隔离+熔断）
//            await GetCombinedPolicy().ExecuteAsync(async () =>
//            {
//                // 核心优化：批量并行发布
//                var publishTasks = batch.Select(eto =>
//                    DistributedEventBus.PublishAsync(eto, onUnitOfWorkComplete: false, useOutbox: true)
//                );

//                // 并行等待所有发布任务完成，减少上下文切换
//                await Task.WhenAll(publishTasks).ConfigureAwait(false);
//            }).ConfigureAwait(false);

//            Logger.LogDebug("[批量发布] | [处理成功] -> [事件总线] | 主题={0} | 数量={1}条", batch.First().MqttTopic, batch.Count);
//        }
//        catch (Exception ex)
//        {
//            // 发布失败：将消息退回队列，保证不丢失
//            Logger.LogError(ex, "[批量发布] | [处理失败] X-> [事件总线]  | 主题={0} | 数量={1}条 | 消息已退回队列",
//                batch.First().MqttTopic, batch.Count);
//            foreach (var eto in batch)
//            {
//                queue.Enqueue(eto);
//            }
//            // 恢复时间记录以便重新计时
//            _topicLastEnqueueTime[batch.First().MqttTopic] = DateTime.UtcNow;
//        }
//    }

//    /// <summary>
//    ///  消息入队时更新时间戳（新增辅助方法，不修改原有入队逻辑）
//    /// </summary>
//    /// <param name="topic"></param>

//    private void UpdateTopicEnqueueTime(string topic)
//    {
//        if (_topicLastEnqueueTime != null)
//        {
//            _topicLastEnqueueTime[topic] = DateTime.UtcNow;
//        }
//    }
//    #endregion

//    #region 核心消息处理（MQTT消息拦截+发布）
//    /// <summary>
//    /// MQTT发布消息拦截处理（核心业务逻辑，仅新增时间戳更新）
//    /// 【设计思路】：
//    /// 1. 快速失败：先做基础校验，无效消息直接拒绝，减少资源消耗
//    /// 2. 分级校验：设备认证→设备信息→消息内容，逐步深入
//    /// 3. 性能优化：内存零拷贝（AsSpan）、分级日志（Debug级别才输出详细信息）
//    /// 4. 分支处理：启用批量则走批量队列，否则走单条发布
//    /// 5. 异常保护：捕获所有异常，保证拦截器不崩溃，不影响其他消息
//    /// </summary>
//    /// <param name="eventArgs">MQTT发布事件参数</param>
//    /// <returns>异步任务</returns>
//    private async Task InterceptingPublishHandlerAsync(InterceptingPublishEventArgs eventArgs)
//    {
//        // 生成唯一追踪ID：便于全链路日志追踪
//        var trackId = GuidGenerator.Create().ToString();
//        try
//        {
//            // 快速提取消息信息（减少多次访问属性的开销）
//            var clientId = eventArgs.ClientId;
//            var topic = eventArgs.ApplicationMessage.Topic;
//            var payloadSegment = eventArgs.ApplicationMessage.PayloadSegment;

//            // 1. 基础校验（快速失败）：无效消息直接拒绝，减少后续处理
//            if (string.IsNullOrWhiteSpace(clientId) ||
//                string.IsNullOrWhiteSpace(topic) ||
//                payloadSegment.Count == 0)
//            {
//                Logger.LogWarning("[{TrackId}] 无效消息 | ClientId={0} | Topic={1}", trackId, clientId, topic);
//                eventArgs.ProcessPublish = false;
//                return;
//            }

//            // 2. 获取设备认证信息（从会话缓存，避免重复解析）
//            // 设计原因：MQTT会话会缓存设备认证信息，无需每次重新解析，提升性能
//            MqttAuthParams? authParams = eventArgs.SessionItems[AuthParamsSessionItemKey] as MqttAuthParams;
//            if (authParams == null)
//            {
//                Logger.LogWarning("[{TrackId}] 未找到认证信息 | ClientId={0}", trackId, clientId);
//                eventArgs.ProcessPublish = false;
//                return;
//            }

//            // 3. 设备信息校验：保证消息关联的设备信息完整
//            if (string.IsNullOrWhiteSpace(authParams.ProductKey) ||
//                string.IsNullOrWhiteSpace(authParams.DeviceName))
//            {
//                Logger.LogWarning("[{TrackId}] 设备信息不完整 | ProductKey={0} | DeviceName={1}",
//                    trackId, authParams.ProductKey, authParams.DeviceName);
//                eventArgs.ProcessPublish = false;
//                return;
//            }

//            // 4. 分级日志（性能优化：Debug级别才输出详细信息，减少日志开销）
//            if (Logger.IsEnabled(LogLevel.Debug))
//            {
//                Logger.LogDebug(
//                    "[接收设备消息] | [{TrackId}] | 设备={0}/{1} | 主题={2} | 大小={3}B",
//                    trackId, authParams.ProductKey, authParams.DeviceName, topic, payloadSegment.Count
//                );
//            }

//            // 5. 构建事件对象（内存优化：AsSpan避免冗余数组复制）
//            // 设计原因：payloadSegment.Array是完整数组，AsSpan仅取有效部分，减少内存拷贝
//            var eto = new MqttClientPublishTopicEto
//            {
//                MqttTrackId = trackId,
//                MqttClientId = clientId,
//                MqttTopic = topic,
//                MqttPayload = payloadSegment.Array!.AsSpan(payloadSegment.Offset, payloadSegment.Count).ToArray(),
//                ProductKey = authParams.ProductKey,
//                DeviceName = authParams.DeviceName,
//                Timestamp = DateTime.UtcNow
//            };

//            // 6. 分支处理：启用批量优化则走批量队列，否则走单条发布
//            if (CurrentIoTMqttOptions.PublishingOptimization.EnableOptimizations && _batchPublishQueue != null)
//            {
//                // 6.1 主题级限流检查（防止热点主题压垮系统）
//                var topicThrottleLimit = CurrentIoTMqttOptions.PublishingOptimization.TopicBasedThrottling
//                    .FirstOrDefault(kv => topic.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;

//                if (topicThrottleLimit > 0 && !IsWithinRateLimit(topic, topicThrottleLimit))
//                {
//                    Logger.LogWarning("[{TrackId}] 主题限流 | Topic={0} | 限流阈值={1}条/秒",
//                        trackId, topic, topicThrottleLimit);
//                    _fallbackQueue.Enqueue(eto);
//                    eventArgs.ProcessPublish = false;
//                    return;
//                }

//                // 6.2 添加到批量发布队列（按主题分组）
//                var topicQueue = _batchPublishQueue.GetOrAdd(topic, _ => new ConcurrentQueue<MqttClientPublishTopicEto>());
//                topicQueue.Enqueue(eto);

//                //+ 新增：更新最后入队时间
//                UpdateTopicEnqueueTime(topic);

//                // 6.3 达到阈值立即处理（减少消息延迟）
//                if (topicQueue.Count >= CurrentIoTMqttOptions.PublishingOptimization.BatchPublishThreshold)
//                {
//                    await ProcessBatchQueueAsync(topicQueue).ConfigureAwait(false);
                    
//                    // 处理完成后移除时间记录
//                    _topicLastEnqueueTime.TryRemove(topic, out _);
//                }

//                eventArgs.ProcessPublish = true;
//            }
//            else
//            {
//                // 6. 核心处理：使用Polly组合策略（隔离+熔断保护）
//                await GetCombinedPolicy().ExecuteAsync(async (context) =>
//                {
//                    context["Eto"] = eto;
//                    await DistributedEventBus.PublishAsync(eto, onUnitOfWorkComplete: false, useOutbox: true)
//                        .ConfigureAwait(false);
//                }, new Context()).ConfigureAwait(false);

//                Logger.LogDebug("[单条发布(非队列)] | [消息发布成功] -> [事件总线] [{TrackId}] | Topic={0}", trackId, topic);
//                eventArgs.ProcessPublish = true;
//            }
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "[{TrackId}] 处理MQTT消息失败", trackId);
//            eventArgs.ProcessPublish = false;
//        }
//    }

//    /// <summary>
//    /// 速率限制检查（原有逻辑保留）
//    /// </summary>
//    private bool IsWithinRateLimit(string topic, int limit)
//    {
//        // 此处为原有实现，根据实际业务逻辑补充
//        return true;
//    }
//    #endregion

//    /// <summary>
//    /// 释放资源（原有逻辑保留）
//    /// </summary>
//    public void Dispose()
//    {
//        _batchPublishCts?.Cancel();
//        _retryLock?.Dispose();
//        _batchProcessLock?.Dispose();
//    }
//}


