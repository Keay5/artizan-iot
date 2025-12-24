//using Artizan.IoT.Mqtts.Messages;
//using Artizan.IoT.Things.Caches;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Polly;
//using Polly.Retry;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Volo.Abp.Caching;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Settings;

//namespace Artizan.IoTHub.Mqtts.Messages.PostProcessors.Caches;

///// <summary>
///// MQTT消息缓存后处理器（核心业务组件）
///// 
///// 【整体设计理念】
///// 1. 高性能：通过批量写入减少Redis交互次数，降低网络开销和Redis压力
///// 2. 高可靠：重试策略应对Redis临时故障，失败重入队避免数据丢失
///// 3. 易运维：配置热更新无需重启服务，完善的日志便于问题排查
///// 4. 线程安全：通过并发容器、锁机制保证多线程环境下的数据一致性
///// 
///// 【核心设计模式】
///// 1. 单例模式：通过ABP的ISingletonDependency标识，确保全局唯一实例，共享队列/定时器等资源
///// 2. 生产者-消费者模式：ConcurrentQueue作为消息缓冲区，生产者入队、消费者批量处理
///// 3. 策略模式：Polly重试策略抽象化，可灵活替换重试规则（如指数退避、固定间隔）
///// 4. 托管服务模式：实现IHostedService，由ABP框架管理生命周期（启动/停止）
///// 
///// 【核心职责】
///// 接收MQTT解析后的设备属性数据，批量写入Redis分布式缓存，维护：
///// - 最新值缓存：按设备维度存储最新一条数据，供业务快速查询
///// - 历史值缓存：按设备维度存储近期数据，支持时间范围查询（自动过滤超期数据）
///// </summary>
//public class MqttCacheMessagePostProcessor_Backup2 :
//    IMqttCacheMessagePostProcessor<MqttMessageContext>,
//    ISingletonDependency,  // ABP单例标识：全局唯一实例
//    IHostedService,        // 托管服务：ABP自动管理启动/停止
//    IDisposable            // 资源释放：确保非托管资源正确回收
//{
//    #region 核心字段（单例共享资源，线程安全设计）
//    /// <summary>
//    /// 最新属性数据分布式缓存（ABP泛型封装）
//    /// 【设计考量】
//    /// - 使用ABP的IDistributedCache<T>：自动完成JSON序列化/反序列化，无需手动处理
//    /// - 泛型类型匹配缓存数据结构：ThingPropertyDataCacheItem为设备属性缓存项标准模型
//    /// - 缓存键规则：产品Key+设备Name（唯一标识单设备）
//    /// </summary>
//    private readonly IDistributedCache<ThingPropertyDataCacheItem> _propertyLatestCache;

//    /// <summary>
//    /// 历史属性数据分布式缓存（ABP泛型封装）
//    /// 【设计考量】
//    /// - 缓存值为List<T>：存储单设备多条历史数据，支持时间范围查询
//    /// - 缓存键规则：产品Key+设备Name + ":History"（与最新值缓存键区分）
//    /// - 自动过滤超期数据：避免缓存无限膨胀，降低Redis内存占用
//    /// </summary>
//    private readonly IDistributedCache<List<ThingPropertyDataCacheItem>> _propertyHistoryCache;

//    /// <summary>
//    /// ABP配置提供者（动态读取配置）
//    /// 【设计考量】
//    /// - 支持配置热更新：无需重启服务即可调整批量阈值、超时时间等核心参数
//    /// - 适配ABP配置体系：可从配置文件、配置中心、数据库等多源读取配置
//    /// </summary>
//    private readonly ISettingProvider _settingProvider;

//    /// <summary>
//    /// 日志组件（结构化日志）
//    /// 【设计考量】
//    /// - 按TraceId追踪：关联单条MQTT消息的全链路处理日志
//    /// - 分级日志：Debug（调试）、Info（正常流程）、Warning（非致命异常）、Error（致命异常）
//    /// - 关键指标记录：批量处理数量、耗时、剩余队列数，便于性能监控
//    /// </summary>
//    private readonly ILogger<MqttCacheMessagePostProcessor_Backup2> _logger;

//    /// <summary>
//    /// 批量数据队列（线程安全队列）
//    /// 【设计模式】生产者-消费者模式核心载体
//    /// 【设计考量】
//    /// - ConcurrentQueue：多线程入队/出队无锁安全，避免数据竞争
//    /// - 缓冲区作用：削峰填谷，避免MQTT消息峰值直接冲击Redis
//    /// - 无界队列：理论上无容量限制（实际受内存约束），避免数据丢失
//    /// </summary>
//    private readonly ConcurrentQueue<ThingPropertyDataCacheItem> _batchQueue = new ConcurrentQueue<ThingPropertyDataCacheItem>();

//    /// <summary>
//    /// 批量写入锁（信号量）
//    /// 【设计考量】
//    /// - SemaphoreSlim(1,1)：确保同一时间只有1个批量写入操作执行
//    /// - 避免并发写入冲突：防止多线程同时处理队列导致数据重复/丢失
//    /// - 支持取消：WaitAsync(cancellationToken)避免死锁
//    /// </summary>
//    private readonly SemaphoreSlim _batchLock = new SemaphoreSlim(1, 1);

//    /// <summary>
//    /// 定时器读写锁
//    /// 【设计考量】
//    /// - ReaderWriterLockSlim：读锁共享、写锁独占，兼顾并发性能
//    /// - 解决PeriodicTimer不可变问题：替换定时器时保证线程安全
//    /// - 读场景：批量超时循环读取定时器实例；写场景：配置更新时替换定时器
//    /// </summary>
//    private readonly ReaderWriterLockSlim _timerLock = new ReaderWriterLockSlim();

//    /// <summary>
//    /// 配置刷新定时器（固定30秒间隔）
//    /// 【设计考量】
//    /// - PeriodicTimer：.NET6+高性能定时器，无内存泄漏风险
//    /// - 固定间隔：30秒刷新一次配置，平衡配置实时性和性能开销
//    /// </summary>
//    private readonly PeriodicTimer _settingsRefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

//    /// <summary>
//    /// 重试策略（Polly）
//    /// 【设计模式】策略模式
//    /// 【设计考量】
//    /// - 应对Redis临时故障：网络抖动、Redis主从切换等场景自动重试
//    /// - 指数退避重试：100ms→200ms→300ms，避免短时间内重复冲击Redis
//    /// - 重试日志：记录重试次数和延迟，便于排查Redis稳定性问题
//    /// </summary>
//    private readonly AsyncRetryPolicy _retryPolicy;

//    /// <summary>
//    /// 批量超时定时器（动态可替换）
//    /// 【设计考量】
//    /// - PeriodicTimer：触发批量写入（即使队列未达阈值），保证数据及时性
//    /// - 动态替换：配置更新时销毁旧实例、创建新实例，实现超时间隔热更新
//    /// </summary>
//    private PeriodicTimer? _batchTimer;

//    /// <summary>
//    /// 资源释放标识
//    /// 【设计考量】
//    /// - 确保Dispose方法幂等性：避免重复释放资源导致异常
//    /// - 配合_isRunning：双重校验处理器状态，防止已释放后执行操作
//    /// </summary>
//    private bool _disposed = false;

//    /// <summary>
//    /// 处理器运行状态标识
//    /// 【设计考量】
//    /// - 控制后台任务循环：启动后为true，停止后为false，终止定时器循环
//    /// - 防止重复启动：StartAsync中校验，避免多次调用StartAsync导致异常
//    /// </summary>
//    private bool _isRunning = false;

//    #region 配置项（默认值+热更新）
//    /// <summary>
//    /// 批量写入阈值
//    /// 【设计考量】
//    /// - 默认100条：平衡批量性能和数据延迟（阈值越大，单次写入效率越高，延迟越大）
//    /// - 热更新：可根据业务场景调整（如高并发场景调大，低延迟场景调小）
//    /// </summary>
//    private int _batchSize = 100;

//    /// <summary>
//    /// 批量写入超时时间（秒）
//    /// 【设计考量】
//    /// - 默认1秒：即使队列未达阈值，1秒内也会触发写入，保证数据及时性
//    /// - 热更新：适配不同业务的实时性要求（如实时监控场景调小至0.5秒）
//    /// </summary>
//    private int _batchTimeoutSeconds = 1;

//    /// <summary>
//    /// 最新值缓存过期时间（秒）
//    /// 【设计考量】
//    /// - 默认3600秒（1小时）：最新值需长期缓存，供业务查询设备当前状态
//    /// - 绝对过期：避免缓存永久存在，降低Redis内存占用
//    /// </summary>
//    private int _latestDataExpireSeconds = 3600;

//    /// <summary>
//    /// 历史数据保留时长（秒）
//    /// 【设计考量】
//    /// - 默认900秒（15分钟）：仅保留近期历史数据，避免缓存膨胀
//    /// - 双重过滤：写入时过滤超期数据 + 缓存过期，双重保证数据时效性
//    /// </summary>
//    private int _historyDataRetainSeconds = 900;
//    #endregion
//    #endregion

//    #region 构造函数（依赖注入+初始化）
//    /// <summary>
//    /// 构造函数（依赖注入）
//    /// 【设计理念】
//    /// - 构造函数注入：符合依赖倒置原则，依赖抽象（接口）而非具体实现
//    /// - 便于单元测试：可注入模拟缓存、模拟配置提供者，隔离测试核心逻辑
//    /// - 初始化一次性资源：重试策略、日志初始化等，避免重复创建
//    /// </summary>
//    /// <param name="propertyLatestCache">最新值分布式缓存</param>
//    /// <param name="propertyHistoryCache">历史数据分布式缓存</param>
//    /// <param name="settingProvider">ABP配置提供者</param>
//    /// <param name="logger">日志组件</param>
//    public MqttCacheMessagePostProcessor_Backup2(
//        IDistributedCache<ThingPropertyDataCacheItem> propertyLatestCache,
//        IDistributedCache<List<ThingPropertyDataCacheItem>> propertyHistoryCache,
//        ISettingProvider settingProvider,
//        ILogger<MqttCacheMessagePostProcessor_Backup2> logger)
//    {
//        // 依赖赋值（必须保证所有依赖非空，ABP容器会自动注入）
//        _propertyLatestCache = propertyLatestCache;
//        _propertyHistoryCache = propertyHistoryCache;
//        _settingProvider = settingProvider;
//        _logger = logger;

//        // 初始化重试策略（Polly）
//        _retryPolicy = Policy
//            .Handle<Exception>() // 捕获所有异常（可按需缩小范围：如RedisConnectionException）
//            .WaitAndRetryAsync(
//                retryCount: 3, // 最大重试次数：3次（平衡可靠性和重试开销）
//                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt), // 指数退避
//                onRetry: (ex, delay, retryCount) =>
//                {
//                    // 重试日志：包含异常信息、重试次数、延迟时间，便于排查
//                    _logger.LogWarning(ex, $"Redis写入重试[{retryCount}/3]，延迟{delay.TotalMilliseconds}ms：{ex.Message}");
//                });

//        _logger.LogInformation("MQTT消息缓存处理器（单例）初始化完成（基于ABP分布式缓存）");
//    }
//    #endregion

//    #region IHostedService 实现（生命周期管理）
//    /// <summary>
//    /// 启动处理器（ABP框架自动调用）
//    /// 【设计思路】
//    /// 1. 首次加载配置：从ABP配置中心读取最新配置，覆盖默认值
//    /// 2. 初始化定时器：创建批量超时定时器，按初始配置设置间隔
//    /// 3. 启动后台任务：配置刷新循环、批量超时触发循环，独立线程执行
//    /// 4. 异常处理：启动失败时记录日志并抛出，告知ABP启动失败
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌（应用停止时触发）</param>
//    /// <returns>异步任务</returns>
//    public async Task StartAsync(CancellationToken cancellationToken = default)
//    {
//        // 防止重复启动：已运行则直接返回
//        if (_isRunning)
//        {
//            _logger.LogDebug("MQTT消息缓存处理器已启动，无需重复启动");
//            return;
//        }

//        try
//        {
//            // 1.先创建定时器，后标记运行状态，首次加载配置（从ABP Setting读取，覆盖默认值）
//            await RefreshSettingsAsync(cancellationToken);
//            // 2. 标记为运行中（先标记，再创建定时器，避免锁校验失败）
//            _isRunning = true;

//            // 3. 创建定时器
//            bool timerCreated = CreateBatchTimer(_batchTimeoutSeconds);
//            if (!timerCreated)
//            {
//                throw new InvalidOperationException("批量定时器创建失败，处理器启动异常");
//            }

//            // 3. 启动后台任务（独立线程执行，避免阻塞主线程）
//            // 批量超时触发任务：按配置间隔触发批量写入
//            _ = Task.Run(() => BatchTimeoutTriggerAsync(cancellationToken), cancellationToken);
//            // 配置刷新任务：每30秒刷新一次配置，实现热更新
//            _ = Task.Run(() => SettingsRefreshLoopAsync(cancellationToken), cancellationToken);

//            _logger.LogInformation("MQTT消息缓存处理器启动成功");
//        }
//        catch (Exception ex)
//        {
//            // 启动失败时重置状态
//            _isRunning = false;
//            _logger.LogError(ex, "MQTT消息缓存处理器启动失败");
//            throw; // 抛出异常，告知ABP启动失败（框架会终止应用）
//        }
//    }

//    /// <summary>
//    /// 停止处理器（应用关闭时ABP框架自动调用）
//    /// 【设计思路】
//    /// 1. 标记状态：设置_isRunning=false，终止后台任务循环
//    /// 2. 释放定时器：销毁所有定时器，避免资源泄漏
//    /// 3. 写入剩余数据：将队列中未处理的数据写入Redis，避免数据丢失
//    /// 4. 释放锁资源：释放信号量、读写锁，避免资源泄漏
//    /// 5. 异常处理：记录停止过程中的异常，不影响应用关闭
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    public async Task StopAsync(CancellationToken cancellationToken = default)
//    {
//        // 防止重复停止：未运行则直接返回
//        if (!_isRunning)
//        {
//            _logger.LogDebug("MQTT消息缓存处理器未运行，无需停止");
//            return;
//        }

//        // 1. 标记为停止状态，终止后台任务循环
//        _isRunning = false;

//        try
//        {
//            // 2. 停止所有定时器（释放资源）
//            DisposeBatchTimer(); // 销毁批量超时定时器
//            _settingsRefreshTimer.Dispose(); // 销毁配置刷新定时器

//            // 3. 写入剩余数据（避免应用关闭时队列数据丢失）
//            if (_batchQueue.Count > 0)
//            {
//                _logger.LogInformation($"应用关闭，尝试写入剩余{_batchQueue.Count}条缓存数据");
//                // 最多等待10秒：避免批量写入耗时过长，阻塞应用关闭
//                await TriggerBatchWriteAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
//            }

//            // 4. 释放锁资源
//            _batchLock.Dispose();
//            _timerLock.Dispose();

//            _logger.LogInformation("MQTT消息缓存处理器已停止，资源已释放");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "MQTT消息缓存处理器停止过程中发生异常");
//        }
//    }
//    #endregion

//    #region IMessagePostProcessor 实现（MQTT消息处理入口）
//    /// <summary>
//    /// 处理器优先级（值越大，执行顺序越靠后）
//    /// 【设计考量】
//    /// - 优先级50：确保缓存处理器在消息解析、验证等前置处理器之后执行
//    /// - 避免处理未解析/验证失败的数据，保证缓存数据的有效性
//    /// </summary>
//    public int Priority => 50;

//    /// <summary>
//    /// 处理器启用状态（从ABP配置动态读取）
//    /// 【设计考量】
//    /// - 支持动态启用/禁用：无需重启服务，便于运维调试（如排查缓存问题时临时禁用）
//    /// - 同步读取：Result不会阻塞（ABP SettingProvider已做缓存优化）
//    /// </summary>
//    public bool IsEnabled => _settingProvider.GetAsync<bool>("Artizan.IoT.Message.Cache.Enabled").Result;

//    /// <summary>
//    /// 消息处理核心方法（生产者逻辑）
//    /// 【设计思路】
//    /// 1. 状态校验：处理器未运行/已释放则跳过，避免无效操作
//    /// 2. 数据校验：上下文无效（未解析成功/无数据）则跳过，保证数据有效性
//    /// 3. 构建缓存项：标准化缓存数据结构，统一数据格式
//    /// 4. 入队操作：将缓存项加入批量队列，生产者逻辑核心
//    /// 5. 阈值触发：队列数量达阈值则触发批量写入，消费者逻辑触发
//    /// 6. 异常处理：记录异常并更新消息处理状态，便于全链路追踪
//    /// </summary>
//    /// <param name="context">MQTT消息上下文（包含解析后的数据、TraceId等）</param>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    public async Task ProcessAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
//    {
//        // 状态校验：处理器未运行/已释放，跳过写入
//        if (!_isRunning || _disposed)
//        {
//            _logger.LogWarning($"[{context.TraceId}] MQTT消息缓存处理器未启动/已释放，跳过写入");
//            return;
//        }

//        try
//        {
//            // 数据校验：上下文无效（未解析成功/无数据），跳过缓存写入
//            // 修复点：移除不存在的context.IsDisposed属性
//            if (!context.IsParsedSuccess || context.ParsedData == null)
//            {
//                _logger.LogWarning($"[{context.TraceId}] MQTT消息上下文无效（未解析成功/无数据），跳过缓存写入");
//                return;
//            }

//            // 构建缓存项：标准化格式
//            var cacheItem = new ThingPropertyDataCacheItem(
//                productKey: context.ProductKey,
//                deviceName: context.DeviceName,
//                data: context.ParsedData,
//                timestampUtcMs: new DateTimeOffset(context.ReceiveTimeUtc).ToUnixTimeMilliseconds());

//            // 入队操作（生产者逻辑）：ConcurrentQueue.Enqueue线程安全
//            _batchQueue.Enqueue(cacheItem);
//            var queueCount = _batchQueue.Count;
//            _logger.LogDebug($"[{context.TraceId}] 设备[{cacheItem.ProductKey}/{cacheItem.DeviceName}]数据加入缓存队列，当前队列数：{queueCount}");

//            // 阈值触发：达到批量阈值则触发批量写入
//            if (queueCount >= _batchSize)
//            {
//                await TriggerBatchWriteAsync(cancellationToken);
//            }
//        }
//        catch (Exception ex)
//        {
//            // 异常日志：包含TraceId，便于关联单条消息的全链路日志
//            _logger.LogError(ex, $"[{context.TraceId}] MQTT消息缓存处理失败：{ex.Message}");

//            // 更新消息处理步骤结果：便于追踪整体消息流程状态
//            context.UpdateStepResult(
//                stepName: "MqttMessageCachePostProcessor",
//                isSuccess: false,
//                elapsed: TimeSpan.Zero,
//                errorMsg: ex.Message,
//                exception: ex);
//        }
//    }
//    #endregion

//    #region 定时器管理（解决PeriodicTimer无Change方法问题）
//    /// <summary>
//    /// 创建批量超时定时器（销毁旧实例+创建新实例）
//    /// 【设计思路】
//    /// - PeriodicTimer无Change方法：通过"销毁旧实例+创建新实例"实现间隔动态调整
//    /// - 写入锁保护：确保替换过程中无并发操作，避免空引用/重复创建
//    /// - 先销毁后创建：避免旧定时器继续触发，导致重复批量写入
//    /// </summary>
//    /// <param name="timeoutSeconds">新的超时间隔（秒）</param>
//    private bool CreateBatchTimer(int timeoutSeconds)
//    {
//        // 1. 前置校验：已释放则直接返回
//        if (_disposed || !_isRunning)
//        {
//            _logger.LogWarning("处理器已释放/未运行，跳过定时器创建");
//            return false;
//        }

//        bool lockAcquired = false;
//        try
//        {

//            // 2. 带超时的写锁申请（避免死锁，超时1秒）
//            lockAcquired = _timerLock.TryEnterWriteLock(TimeSpan.FromSeconds(1));
//            if (!lockAcquired)
//            {
//                _logger.LogError("获取定时器写锁超时（1秒），跳过定时器创建");
//                return false;
//            }

//            // 3. 双重校验（锁内再次检查状态）
//            if (_disposed || !_isRunning)
//            {
//                return false;
//            }


//            // 4. 调用内部无锁销毁方法
//            DisposeBatchTimerInternal();

//            // 原有创建定时器逻辑（保留）
//            _batchTimer = new PeriodicTimer(TimeSpan.FromSeconds(timeoutSeconds));
//            _logger.LogDebug($"批量定时器已创建，间隔：{timeoutSeconds}秒");
//            return true;
//        }
//        catch (ObjectDisposedException ex)
//        {
//            // 捕获锁已释放的异常（解决核心报错）
//            _logger.LogError(ex, "定时器锁已释放，无法创建定时器");
//            return false;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "创建批量定时器失败");
//            return false;
//        }
//        finally
//        {

//            // 6. 确保锁释放（核心：仅当获取成功时释放）
//            if (lockAcquired)
//            {
//                _timerLock.ExitWriteLock();
//            }
//        }
//    }

//    /// <summary>
//    /// 销毁批量超时定时器
//    /// 【设计思路】
//    /// - 封装销毁逻辑：统一管理定时器生命周期，避免重复销毁
//    /// - 写入锁保护：确保销毁过程原子性，避免并发销毁导致异常
//    /// </summary>
//    private void DisposeBatchTimer()
//    {
//        // 前置校验：已释放则直接返回
//        if (_disposed)
//        {
//            return;
//        }

//        bool lockAcquired = false;
//        try
//        {

//            // 带超时申请写锁（对应报错的446行修复）
//            lockAcquired = _timerLock.TryEnterWriteLock(TimeSpan.FromSeconds(1));
//            if (!lockAcquired)
//            {
//                _logger.LogError("获取定时器写锁超时（1秒），无法销毁定时器");
//                return;
//            }

//            DisposeBatchTimerInternal();
//        }
//        catch (ObjectDisposedException ex)
//        {
//            _logger.LogError(ex, "定时器锁已释放，无法销毁定时器");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "销毁批量定时器失败（外部调用）");
//        }
//        finally
//        {

//            if (lockAcquired)
//            {
//                _timerLock.ExitWriteLock();
//            }
//        }
//    }

//    // 新增内部无锁销毁方法
//    /// <summary>
//    /// 内部销毁定时器（无锁，需外部加锁）
//    /// </summary>
//    private void DisposeBatchTimerInternal()
//    {
//        if (_batchTimer != null)
//        {
//            try
//            {
//                _batchTimer.Dispose();
//                _logger.LogDebug("批量定时器已销毁");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "销毁批量定时器失败");
//            }
//            finally
//            {
//                _batchTimer = null;
//            }
//        }
//    }
//    #endregion

//    #region 批量写入核心逻辑（消费者逻辑+ABP缓存API适配）
//    /// <summary>
//    /// 触发批量写入（阈值触发+超时触发的统一入口）
//    /// 【设计思路】
//    /// 1. 锁保护：确保同一时间只有1个批量写入操作执行
//    /// 2. 数据提取：从队列提取数据（最多_batchSize条），避免单次处理过多
//    /// 3. 重试写入：通过Polly重试策略写入Redis，提升可靠性
//    /// 4. 失败重入队：写入失败时将数据重新入队，避免数据丢失
//    /// 5. 锁释放：finally块释放锁，确保锁不会泄露
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    public async Task TriggerBatchWriteAsync(CancellationToken cancellationToken = default)
//    {
//        // 前置校验：已释放/未运行则直接返回
//        if (_disposed || !_isRunning)
//        {
//            _logger.LogWarning("处理器已释放/未运行，跳过批量写入");
//            return;
//        }

//        // 等待获取锁（支持取消，避免死锁）
//        await _batchLock.WaitAsync(cancellationToken);

//        // 定义批量数据列表：扩大作用域，使catch块可访问（失败重入队）
//        var batchData = new List<ThingPropertyDataCacheItem>();

//        try
//        {
//            // 双重校验：确保队列非空且处理器处于运行状态
//            if (_batchQueue.IsEmpty || !_isRunning)
//            {
//                return;
//            }

//            // 从队列提取数据：最多提取_batchSize条，避免单次处理过多导致超时
//            while (batchData.Count < _batchSize && _batchQueue.TryDequeue(out var data))
//            {
//                batchData.Add(data);
//            }

//            // 无数据则直接返回
//            if (batchData.Count == 0)
//            {
//                return;
//            }

//            // 批量写入Redis（带重试机制）
//            var stopwatch = Stopwatch.StartNew(); // 记录耗时，便于性能监控
//            await _retryPolicy.ExecuteAsync(async () =>
//            {
//                await WriteToRedisAsync(batchData, cancellationToken);
//            });
//            stopwatch.Stop();

//            // 批量写入完成日志：包含处理数量、耗时、剩余队列数，便于监控
//            _logger.LogInformation($"Redis批量写入完成 | 处理数据量：{batchData.Count} | 耗时：{stopwatch.Elapsed.TotalMilliseconds:F2}ms | 剩余队列数：{_batchQueue.Count}");
//        }
//        catch (Exception ex)
//        {
//            // 异常日志：记录批量写入失败原因
//            _logger.LogError(ex, $"Redis批量写入失败：{ex.Message}");

//            // 失败重入队：将未写入的数据重新加入队列，避免数据丢失
//            foreach (var data in batchData)
//            {
//                _batchQueue.Enqueue(data);
//            }
//        }
//        finally
//        {
//            // 确保释放锁：即使发生异常，也能释放锁，避免死锁
//            if (!_disposed)
//            {
//                _batchLock.Release();
//            }
//        }
//    }

//    /// <summary>
//    /// 批量写入Redis（核心实现，适配ABP缓存API）
//    /// 【设计思路】
//    /// 1. 最新值处理：按设备分组取最新一条，批量写入（减少重复数据）
//    /// 2. 历史值处理：批量读取现有历史数据，追加新数据并过滤超期数据
//    /// 3. ABP API适配：
//    ///    - SetManyAsync：使用DistributedCacheEntryOptions传参（而非直接TimeSpan）
//    ///    - GetManyAsync：返回数组转Dictionary，支持TryGetValue调用
//    /// 4. 性能优化：批量API减少Redis交互次数（N次单条→1次批量）
//    /// </summary>
//    /// <param name="batchData">待写入的批量数据</param>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    private async Task WriteToRedisAsync(List<ThingPropertyDataCacheItem> batchData, CancellationToken cancellationToken)
//    {
//        // 计算时间戳：用于过滤超期历史数据
//        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
//        var latestExpireTime = TimeSpan.FromSeconds(_latestDataExpireSeconds);
//        var historyRetainThreshold = now - (_historyDataRetainSeconds * 1000); // 毫秒级阈值

//        // ========== 1. 批量处理最新值（按设备分组，取最新一条） ==========
//        var latestDataGroup = batchData
//            .GroupBy(d => (d.ProductKey, d.DeviceName)) // 按产品+设备分组
//            .Select(g => g.OrderByDescending(d => d.TimestampUtcMs).First()) // 每个设备取最新一条
//            .ToList();

//        // 构建批量写入字典（替换ABP不存在的DistributedCacheKeyAndValue）
//        var latestCacheDict = new Dictionary<string, ThingPropertyDataCacheItem>();
//        foreach (var data in latestDataGroup)
//        {
//            var cacheKey = ThingPropertyDataCacheItem.CalculateCacheKey(data.ProductKey, data.DeviceName);
//            latestCacheDict[cacheKey] = data;
//        }

//        // ABP批量写入最新值（修复点：使用DistributedCacheEntryOptions传参）
//        await _propertyLatestCache.SetManyAsync(
//            latestCacheDict, new DistributedCacheEntryOptions
//            {
//                AbsoluteExpirationRelativeToNow = latestExpireTime
//            },
//            token: cancellationToken
//        );
//        _logger.LogDebug($"批量写入最新值完成 | 涉及设备数：{latestCacheDict.Count}");

//        // ========== 2. 批量处理历史数据（按设备分组，批量更新） ==========
//        var historyDataGroup = batchData.GroupBy(d => (d.ProductKey, d.DeviceName)).ToList();

//        // 构建所有历史数据的缓存键（产品+设备 + ":History"）
//        var historyCacheKeys = historyDataGroup
//            .Select(g => $"{ThingPropertyDataCacheItem.CalculateCacheHistoryKey(g.Key.ProductKey, g.Key.DeviceName)}")
//            .ToList();

//        // ABP批量读取现有历史数据（修复点：移除不支持的cancellationToken参数）
//        var existingHistoryArray = await _propertyHistoryCache.GetManyAsync(historyCacheKeys);

//        // 修复点：数组转Dictionary，支持TryGetValue调用
//        var existingHistoryDict = existingHistoryArray.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

//        // 构建批量写入的历史数据字典
//        var historyCacheDict = new Dictionary<string, List<ThingPropertyDataCacheItem>>();
//        foreach (var group in historyDataGroup)
//        {
//            var historyKey = $"{ThingPropertyDataCacheItem.CalculateCacheHistoryKey(group.Key.ProductKey, group.Key.DeviceName)}";

//            // 读取现有历史数据（无则初始化空列表）
//            existingHistoryDict.TryGetValue(historyKey, out var existingHistory);
//            var currentHistory = existingHistory ?? new List<ThingPropertyDataCacheItem>();

//            // 追加新数据 + 过滤超期数据（避免历史数据无限增长）
//            var newHistory = currentHistory
//                .Concat(group) // 追加新数据
//                .Where(d => d.TimestampUtcMs >= historyRetainThreshold) // 过滤超期数据
//                .OrderBy(d => d.TimestampUtcMs) // 按时间排序，便于查询
//                .ToList();

//            // 加入批量写入字典
//            historyCacheDict[historyKey] = newHistory;
//        }

//        // ABP批量写入历史数据（修复点：使用DistributedCacheEntryOptions传参）
//        await _propertyHistoryCache.SetManyAsync(
//            historyCacheDict, new DistributedCacheEntryOptions
//            {
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_historyDataRetainSeconds)
//            },
//            token: cancellationToken
//        );
//        _logger.LogDebug($"批量写入历史数据完成 | 涉及设备数：{historyCacheDict.Count} | 过滤后历史数据总条数：{historyCacheDict.Sum(x => x.Value.Count)}");
//    }
//    #endregion

//    #region 配置刷新 & 后台任务（热更新+超时触发）
//    /// <summary>
//    /// 配置刷新循环（后台任务）
//    /// 【设计思路】
//    /// 1. 循环条件：处理器运行中 + 未取消，确保任务持续运行
//    /// 2. 定时器触发：每30秒触发一次配置刷新，平衡实时性和性能
//    /// 3. 异常处理：捕获取消异常（正常停止）、其他异常（记录日志不终止循环）
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    private async Task SettingsRefreshLoopAsync(CancellationToken cancellationToken)
//    {
//        try
//        {
//            // 循环条件：处理器运行中 + 未取消
//            while (_isRunning && !cancellationToken.IsCancellationRequested)
//            {
//                // 等待定时器触发（30秒间隔）
//                if (await _settingsRefreshTimer.WaitForNextTickAsync(cancellationToken))
//                {
//                    await RefreshSettingsAsync(cancellationToken);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // 取消异常：应用停止时正常触发，记录日志即可
//            _logger.LogInformation("配置刷新任务已取消（应用停止）");
//        }
//        catch (Exception ex)
//        {
//            // 其他异常：记录日志但不终止循环，确保配置刷新功能持续可用
//            _logger.LogError(ex, "配置刷新任务发生异常（不影响处理器运行）");
//        }
//    }

//    /// <summary>
//    /// 刷新配置（核心：从ABP Setting读取最新配置）
//    /// 【设计思路】
//    /// 1. 读取配置：从ABP Setting读取最新配置，无配置则使用默认值
//    /// 2. 动态调整：批量超时时间变更时，重建定时器实现间隔热更新
//    /// 3. 异常处理：读取失败时使用旧配置，确保处理器正常运行
//    /// 4. 配置日志：记录刷新后的配置，便于运维核对
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    private async Task RefreshSettingsAsync(CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            // 从ABP Setting读取配置（键与appsettings.json对应）
//            _batchSize = int.Parse(await _settingProvider.GetOrNullAsync("Artizan.IoT.Message.Cache.BatchSize") ?? "100");
//            var newTimeoutSeconds = int.Parse(await _settingProvider.GetOrNullAsync("Artizan.IoT.Message.Cache.BatchTimeoutSeconds") ?? "1");
//            _latestDataExpireSeconds = int.Parse(await _settingProvider.GetOrNullAsync("Artizan.IoT.Message.Cache.LatestDataExpireSeconds") ?? "3600");
//            _historyDataRetainSeconds = int.Parse(await _settingProvider.GetOrNullAsync("Artizan.IoT.Message.Cache.HistoryDataRetainSeconds") ?? "900");

//            // 批量超时时间变更时，重建定时器（动态调整间隔）
//            if (newTimeoutSeconds != _batchTimeoutSeconds && _isRunning)
//            {
//                _batchTimeoutSeconds = newTimeoutSeconds;
//                CreateBatchTimer(_batchTimeoutSeconds);
//            }

//            // 配置刷新日志：记录所有配置项，便于运维核对
//            _logger.LogDebug("配置已刷新 | 批量阈值：{0} | 超时时间：{1}秒 | 最新值过期时间：{2}秒 | 历史数据保留时长：{3}秒",
//                _batchSize, _batchTimeoutSeconds, _latestDataExpireSeconds, _historyDataRetainSeconds);
//        }
//        catch (Exception ex)
//        {
//            // 配置读取失败：使用旧配置，记录异常便于排查
//            _logger.LogError(ex, "配置刷新失败，继续使用旧配置");
//        }
//    }

//    /// <summary>
//    /// 批量超时触发循环（后台任务）
//    /// 【设计思路】
//    /// 1. 循环条件：处理器运行中 + 未取消，确保任务持续运行
//    /// 2. 定时器读取：读锁读取当前定时器实例，避免与替换操作冲突
//    /// 3. 超时触发：定时器触发且队列非空时，执行批量写入
//    /// 4. 异常处理：捕获取消异常（正常停止）、其他异常（记录日志不终止循环）
//    /// </summary>
//    /// <param name="cancellationToken">取消令牌</param>
//    /// <returns>异步任务</returns>
//    private async Task BatchTimeoutTriggerAsync(CancellationToken cancellationToken)
//    {
//        try
//        {
//            while (_isRunning && !cancellationToken.IsCancellationRequested)
//            {

//                PeriodicTimer? currentTimer = null;
//                bool lockAcquired = false;

//                try
//                {
//                    // 读锁申请带超时
//                    lockAcquired = _timerLock.TryEnterReadLock(TimeSpan.FromSeconds(1));
//                    if (lockAcquired)
//                    {
//                        currentTimer = _batchTimer;
//                    }
//                    else
//                    {
//                        _logger.LogWarning("获取定时器读锁超时（1秒），跳过本次超时检查");
//                        // 等待100ms后重试，避免空循环
//                        await Task.Delay(100, cancellationToken);
//                        continue;
//                    }
//                }
//                catch (ObjectDisposedException ex)
//                {
//                    _logger.LogError(ex, "定时器锁已释放，退出超时触发循环");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "获取定时器读锁失败，跳过本次超时检查");
//                    await Task.Delay(100, cancellationToken);
//                    continue;
//                }
//                finally
//                {
//                    if (lockAcquired)
//                    {
//                        _timerLock.ExitReadLock();
//                    }
//                }

//                // 原有定时器触发逻辑（保留）
//                if (currentTimer != null && await currentTimer.WaitForNextTickAsync(cancellationToken))
//                {
//                    if (_batchQueue.Count > 0)
//                    {
//                        await TriggerBatchWriteAsync(cancellationToken);
//                    }
//                }
//            }
//        }
//        // 原有异常捕获（保留）
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("批量超时触发任务已取消");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "批量超时触发任务异常");
//        }
//    }
//    #endregion

//    #region 资源释放（IDisposable实现）
//    /// <summary>
//    /// 资源释放（确保非托管资源正确释放）
//    /// 【设计思路】
//    /// 1. 幂等性：通过_disposed标识避免重复释放
//    /// 2. 状态标记：设置_isRunning=false，终止后台任务循环
//    /// 3. 资源释放：销毁所有定时器、锁资源，避免内存泄漏
//    /// 4. 日志记录：记录资源释放完成，便于运维核对
//    /// </summary>
//    public void Dispose()
//    {
//        // 幂等性校验：已释放则直接返回
//        if (_disposed)
//        {
//            return;
//        }

//        // 标记为已释放
//        _disposed = true;
//        _isRunning = false;

//        // 释放资源
//        DisposeBatchTimer(); // 释放批量超时定时器
//        _settingsRefreshTimer.Dispose(); // 释放配置刷新定时器
//        _batchLock.Dispose(); // 释放批量锁
//        _timerLock.Dispose(); // 释放定时器读写锁

//        _logger.LogInformation("MQTT消息缓存处理器（单例）资源已完全释放");
//    }
//    #endregion
//}

