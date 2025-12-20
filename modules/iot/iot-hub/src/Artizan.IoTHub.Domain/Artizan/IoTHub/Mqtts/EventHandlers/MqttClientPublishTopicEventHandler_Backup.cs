//using Artizan.IoT.Mqtts.Etos;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Concurrent;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.EventBus.Distributed;

//namespace Artizan.IoTHub.Mqtts.EventHandlers;

///// <summary>
///// MQTT事件处理器（仅负责：事件接收、并发调度、资源管理）
///// 职责边界：不包含业务逻辑，仅调用MqttMessageProcessingService处理业务
///// </summary>
//[Obsolete("该类已废弃，请使用 MqttClientPublishTopicEventHandler 替代。")]
//public class MqttClientPublishTopicEventHandler_Backup
//    //: IDistributedEventHandler<MqttClientPublishTopicEto>,
//    //  ISingletonDependency,   // 单例依赖：全局仅初始化1次，复用核心资源（锁/通道/消费者），减少GC和线程开销
//    //  IDisposable
//{
//    private readonly ILogger<MqttClientPublishTopicEventHandler_Backup> _logger;

//    #region 核心资源（仅调度/并发相关）
//    private readonly MqttMessageParser _mqttMessageParser; // 注入业务逻辑类

//    /// <summary>
//    /// 带引用计数的信号量包装类
//    /// 设计思路：解决锁被提前Dispose导致的ObjectDisposedException问题
//    /// ReferenceCount：跟踪锁的引用次数，确保所有使用方释放后再清理
//    /// Semaphore：设备级串行处理的核心锁
//    /// </summary>
//    private class SemaphoreWithReferenceCount
//    {
//        /// <summary>
//        /// 设备级串行锁（保证单设备消息有序处理）
//        /// </summary>
//        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

//        /// <summary>
//        /// 引用计数：记录当前持有该锁引用的线程数
//        /// 设计思路：避免锁被提前Dispose，仅当引用计数为0且锁空闲时才清理
//        /// </summary>
//        public int ReferenceCount { get; set; } = 0;
//    }

//    // 设备分区锁。设备维度分区锁：保证单设备消息有序处理，不同设备并行处理 
//    // 选型思路：ConcurrentDictionary是线程安全字典，包装类解决引用计数问题
//    private readonly ConcurrentDictionary<string, SemaphoreWithReferenceCount> _deviceLocks = new();
//    // 保护锁字典操作的同步锁，解决并发场景下的锁生命周期管理问题
//    private readonly object _lockDictionarySync = new object();

//    // 消息处理通道：解耦生产者（事件接收）和消费者（消息处理），避免发布端阻塞
//    // 选型思路：Channel是.NET原生高性能管道，比BlockingCollection更适合异步场景
//    private readonly Channel<MqttClientPublishTopicEto> _messageChannel;

//    // 取消令牌源：用于优雅停止消费者线程，处理服务关闭/重启场景
//    // 设计思路：全局唯一令牌，保证所有消费者统一停止
//    private readonly CancellationTokenSource _cancellationTokenSource = new();

//    // 释放锁：释放标记：防止Dispose被多次调用导致资源重复释放（线程安全关键）
//    private readonly object _disposeLock = new object();
//    // 线程锁：保护Dispose方法的线程安全（单例下可能被多线程调用）
//    private bool _disposed; // 释放标记

//    // 统计无法处理的消息数量（便于监控告警）
//    private long _unprocessedMessageCount = 0;
//    #endregion

//    /// <summary>
//    /// 构造函数（仅初始化调度相关资源）
//    /// </summary>
//    public MqttClientPublishTopicEventHandler_Backup(
//        ILogger<MqttClientPublishTopicEventHandler_Backup> logger,
//        MqttMessageParser mqttMessageParser)
//    {
//        _logger = logger;
//        _mqttMessageParser = mqttMessageParser;

//        // 初始化有界Channel（生产级配置）
//        // 配置有界Channel：避免无限制入队导致内存溢出
//        // 容量建议：根据服务器内存和消息峰值调整（示例10000，8核16G服务器可设50000）
//        _messageChannel = Channel.CreateBounded<MqttClientPublishTopicEto>(new BoundedChannelOptions(10000)
//        {
//            // 队列满策略：等待（可选DropOldest/DropNew，根据业务容忍度调整）
//            // FullMode.Wait：队列满时生产者等待（而非丢消息），保证消息不丢失（生产级核心要求）
//            FullMode = BoundedChannelFullMode.Wait,
//            SingleReader = false,  // 允许多消费者并行读取（核心性能优化点）
//            SingleWriter = false   // 允许多生产者写入（MQTT服务器多线程推送消息）
//        });

//        // 消费者数量：CPU核心数*2（IO密集型场景最优配置，避免CPU过载）
//        // 设计思路：IO密集型任务（网络/数据库操作）线程数可高于CPU核心数，充分利用资源
//        var consumerCount = Environment.ProcessorCount * 2;       // 启动消费者线程（CPU核心数*2）
//        for (int i = 0; i < consumerCount; i++)
//        {
//            // 后台启动消费者：使用_避免编译器警告，不阻塞构造函数（生产级必须）
//            // 传入消费者ID：便于日志追踪哪个消费者处理的消息，定位问题更高效
//            _ = StartConsumerAsync(i, _cancellationTokenSource.Token);
//        }

//        _logger.LogInformation("MQTT消息处理器[{Name}] | 初始化完成，启动{ConsumerCount}个消费者", nameof(MqttClientPublishTopicEventHandler_Backup), consumerCount);
//    }

//    #region 事件接收（生产者）
//    /// <summary>
//    /// ABP事件总线回调入口：接收MQTT发布事件（生产者）,
//    /// 设计目标：仅将消息写入Channel，快速返回，仅做消息入队，不阻塞发布端
//    /// </summary>
//    public async Task HandleEventAsync(MqttClientPublishTopicEto eventData)
//    {
//        try
//        {
//            // 生产级异常处理：捕获并记录异常，避免影响事件总线整体稳定性
//            if (eventData == null)
//            {
//                Interlocked.Increment(ref _unprocessedMessageCount);
//                _logger.LogError("MQTT消息处理器[{Name}] | 接收到空MQTT事件【无法处理】，累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    Interlocked.Read(ref _unprocessedMessageCount));
//                return;
//            }

//            // 消息入队：写入Channel后立即返回，发布端无需等待处理完成
//            // ConfigureAwait(false)：无上下文依赖场景（后台服务）必加，避免捕获同步上下文，提升性能
//            await _messageChannel.Writer.WriteAsync(eventData, _cancellationTokenSource.Token).ConfigureAwait(false);
//            _logger.LogDebug("[消息入队] [成功] | [TrackId:{TrackId}] 消息已写入处理通道 | 设备：{ProductKey}/{DeviceName} | 主题：{Topic}",
//                eventData.MqttTrackId,
//                eventData.ProductKey,
//                eventData.DeviceName,
//                eventData.MqttTopic);
//        }
//        catch (OperationCanceledException)
//        {
//            // 取消异常：服务关闭时正常现象，仅记录警告
//            if (eventData != null)
//            {
//                Interlocked.Increment(ref _unprocessedMessageCount);
//                _logger.LogWarning("[消息入队] [被取消] | MQTT消息处理器[{Name}] | 消息入队被取消（服务关闭）【无法处理】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    eventData.MqttTrackId,
//                    eventData.ProductKey,
//                    eventData.DeviceName,
//                    Interlocked.Read(ref _unprocessedMessageCount));
//            }
//        }
//        catch (Exception ex)
//        {
//            Interlocked.Increment(ref _unprocessedMessageCount);
//            // 未知异常：记录完整堆栈，便于生产环境排查问题
//            _logger.LogError(ex, "[消息入队] [失败] | MQTT消息处理器[{Name}] | 消息入队失败【无法处理】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                nameof(MqttClientPublishTopicEventHandler_Backup),
//                eventData?.MqttTrackId ?? "未知",
//                eventData?.ProductKey ?? "未知",
//                eventData?.DeviceName ?? "未知",
//                Interlocked.Read(ref _unprocessedMessageCount));
//            // 抛出异常：让ABP事件总线触发重试机制（生产级可靠性保障）
//            throw;
//        }
//    }
//    #endregion

//    #region 消费者调度（核心）
//    /// <summary>
//    /// 消费者线程：仅负责并发控制，调用业务服务处理消息，消费者线程入口：并行处理Channel中的消息
//    /// 设计思路：单消费者内串行，多消费者间并行，平衡性能与有序性
//    /// </summary>
//    /// <param name="consumerId">消费者ID（用于日志追踪）</param>
//    /// <param name="cancellationToken">取消令牌（控制消费者停止）</param>
//    /// <returns>异步任务</returns>
//    private async Task StartConsumerAsync(int consumerId, CancellationToken cancellationToken)
//    {
//        // 日志标记：生产环境便于定位消费者启动/停止状态
//        _logger.LogInformation("MQTT消息处理器[{Name}] | [Consumer:{ConsumerId}] 启动", nameof(MqttClientPublishTopicEventHandler_Backup), consumerId);

//        try
//        {
//            // 流式读取Channel：ReadAllAsync是异步迭代器，无阻塞读取消息
//            // ConfigureAwait(false)：避免捕获同步上下文，提升异步性能
//            await foreach (var eventData in _messageChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
//            {
//                // 分区Key：ProductKey+DeviceName，确保同一设备的消息串行处理
//                // 设计思路：避免设备消息乱序（如设备上报的时序数据）
//                var partitionKey = $"{eventData.ProductKey}_{eventData.DeviceName}";
//                SemaphoreSlim semaphore = null;
//                SemaphoreWithReferenceCount semaphoreWithCount = null;
//                // 标记是否成功获取锁，避免对未获取的锁执行Release
//                bool isSemaphoreAcquired = false;
//                // 标记当前消息是否已处理失败
//                bool isMessageFailed = false;

//                try
//                {
//                    #region 1. 原子化获取锁（避免创建/释放竞态）
//                    lock (_lockDictionarySync)
//                    {
//                        if (_disposed)
//                        {
//                            Interlocked.Increment(ref _unprocessedMessageCount);
//                            isMessageFailed = true;
//                            _logger.LogError("[消息通道][Consumer:{ConsumerId}] | 服务已释放【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                                consumerId,
//                                eventData.MqttTrackId,
//                                eventData.ProductKey,
//                                eventData.DeviceName,
//                                Interlocked.Read(ref _unprocessedMessageCount));
//                            // 外层循环的continue，不在finally内，符合语法规则
//                            continue;
//                        }
//                        // 确保获取的锁是未被释放的实例，同时增加引用计数
//                        semaphoreWithCount = _deviceLocks.GetOrAdd(partitionKey, _ => new SemaphoreWithReferenceCount());
//                        semaphoreWithCount.ReferenceCount++; // 引用计数+1，标记有线程持有该锁
//                        semaphore = semaphoreWithCount.Semaphore;
//                        _logger.LogTrace("[消息通道][Consumer:{ConsumerId}][TrackId:{TrackId}] 设备[{PartitionKey}]锁引用计数+1，当前计数：{Count}",
//                            consumerId, eventData.MqttTrackId, partitionKey, semaphoreWithCount.ReferenceCount);
//                    }
//                    #endregion

//                    #region 2. 安全等待锁（检测取消/释放状态）
//                    // 等待前先检查：避免等待已释放的锁
//                    if (_disposed || cancellationToken.IsCancellationRequested)
//                    {
//                        Interlocked.Increment(ref _unprocessedMessageCount);
//                        isMessageFailed = true;
//                        _logger.LogWarning("[消息通道][Consumer:{ConsumerId}] | 服务关闭/取消【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                            consumerId,
//                            eventData.MqttTrackId,
//                            eventData.ProductKey,
//                            eventData.DeviceName,
//                            Interlocked.Read(ref _unprocessedMessageCount));
//                        // 外层循环的continue，不在finally内，符合语法规则
//                        continue;
//                    }

//                    // 带超时的等待：防止无限阻塞，同时检测取消
//                    if (!await semaphore.WaitAsync(1000, cancellationToken).ConfigureAwait(false))
//                    {
//                        Interlocked.Increment(ref _unprocessedMessageCount);
//                        isMessageFailed = true;
//                        _logger.LogError("[消息通道][Consumer:{ConsumerId}] | 锁等待超时（1秒）【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                            consumerId,
//                            eventData.MqttTrackId,
//                            eventData.ProductKey,
//                            eventData.DeviceName,
//                            Interlocked.Read(ref _unprocessedMessageCount));
//                        // 外层循环的continue，不在finally内，符合语法规则
//                        continue;
//                    }
//                    // 标记锁已成功获取，仅在此时才执行Release
//                    isSemaphoreAcquired = true;
//                    #endregion

//                    #region 3. 执行业务逻辑
//                    // 核心修复点：确保业务代码一定会执行（移除可能阻塞的冗余逻辑）
//                    _logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [开始] | [TrackId:{TrackId}] 开始执行业务逻辑 | 设备：{ProductKey}/{DeviceName}",
//                        consumerId,
//                        eventData.MqttTrackId,
//                        eventData.ProductKey,
//                        eventData.DeviceName);

//                    await _mqttMessageParser.ParseTopicMessageAsync(eventData, consumerId, cancellationToken).ConfigureAwait(false);
                  
//                    _logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [完成] | [TrackId:{TrackId}] 业务逻辑执行完成 | 设备：{ProductKey}/{DeviceName}",
//                        consumerId,
//                        eventData.MqttTrackId,
//                        eventData.ProductKey,
//                        eventData.DeviceName);
//                    #endregion
//                }
//                catch (OperationCanceledException)
//                {
//                    Interlocked.Increment(ref _unprocessedMessageCount);
//                    isMessageFailed = true;
//                    // 取消异常：服务关闭时正常现象
//                    _logger.LogWarning("[消息消费][Consumer:{ConsumerId}] [被取消] | 处理被取消【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                        consumerId,
//                        eventData.MqttTrackId,
//                        eventData.ProductKey,
//                        eventData.DeviceName,
//                        Interlocked.Read(ref _unprocessedMessageCount));
//                }
//                catch (Exception ex)
//                {
//                    Interlocked.Increment(ref _unprocessedMessageCount);
//                    isMessageFailed = true;
//                    // 业务异常：记录完整上下文，便于生产环境排查
//                    _logger.LogError(ex, "[消息消费][Consumer:{ConsumerId}] [未处理] | 处理失败【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 主题：{Topic} | 累计无法处理消息数：{UnprocessedCount}",
//                        consumerId,
//                        eventData.MqttTrackId,
//                        eventData.ProductKey,
//                        eventData.DeviceName,
//                        eventData.MqttTopic,
//                        Interlocked.Read(ref _unprocessedMessageCount));
//                }
//                finally
//                {
//                    // 记录最终无法处理的消息（兜底）
//                    if (isMessageFailed)
//                    {
//                        _logger.LogCritical("[消息消费][Consumer:{ConsumerId}] [未处理] | 消息最终判定为无法处理 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 主题：{Topic} | 消息内容摘要：{PayloadSummary}",
//                            consumerId,
//                            eventData.MqttTrackId,
//                            eventData.ProductKey,
//                            eventData.DeviceName,
//                            eventData.MqttTopic,
//                            GetPayloadSummary(eventData.MqttPayload));
//                    }

//                    #region 4. 安全释放锁（核心优化：基于引用计数的释放逻辑）
//                    // 仅在成功获取锁且服务未释放时执行释放
//                    if (isSemaphoreAcquired && semaphore != null && !_disposed)
//                    {
//                        try
//                        {
//                            semaphore.Release();
//                            isSemaphoreAcquired = false; // 释放后重置标记
//                            _logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [锁] | [TrackId:{TrackId}] 锁释放成功 | 设备：{ProductKey}/{DeviceName}",
//                                consumerId,
//                                eventData.MqttTrackId,
//                                eventData.ProductKey,
//                                eventData.DeviceName);
//                        }
//                        catch (ObjectDisposedException ex)
//                        {
//                            Interlocked.Increment(ref _unprocessedMessageCount);
//                            // 捕获锁已释放的预期异常，避免崩溃
//                            _logger.LogError(ex, "[消息消费][Consumer:{ConsumerId}] [锁] [异常] | 锁已被释放（重复释放）【无法处理消息】 | TrackId:{TrackId} | 设备：{ProductKey}/{DeviceName} | 累计无法处理消息数：{UnprocessedCount}",
//                                consumerId,
//                                eventData.MqttTrackId,
//                                eventData.ProductKey,
//                                eventData.DeviceName,
//                                Interlocked.Read(ref _unprocessedMessageCount));
//                        }
//                    }

//                    // 5. 基于引用计数的锁清理逻辑（核心修复：解决提前Dispose问题）
//                    if (semaphoreWithCount != null)
//                    {
//                        lock (_lockDictionarySync)
//                        {
//                            // 引用计数-1，标记当前线程已释放锁
//                            semaphoreWithCount.ReferenceCount--;
//                            _logger.LogTrace("[消息消费][Consumer:{ConsumerId}] [锁] | [TrackId:{TrackId}] 设备[{PartitionKey}]锁引用计数-1，当前计数：{Count}",
//                                consumerId, eventData.MqttTrackId, partitionKey, semaphoreWithCount.ReferenceCount);

//                            // 仅当引用计数为0且锁空闲时，才清理锁（彻底解决提前Dispose问题）
//                            if (!_disposed
//                                && semaphoreWithCount.ReferenceCount == 0
//                                && semaphoreWithCount.Semaphore.CurrentCount == 1
//                                && _deviceLocks.TryGetValue(partitionKey, out var existingSemaphore)
//                                && existingSemaphore == semaphoreWithCount)
//                            {
//                                if (_deviceLocks.TryRemove(partitionKey, out _))
//                                {
//                                    try
//                                    {
//                                        semaphoreWithCount.Semaphore.Dispose(); // 安全释放锁资源
//                                        _logger.LogDebug("[消息消费][Consumer:{ConsumerId}] [锁] | [TrackId:{TrackId}] 清理设备[{PartitionKey}]的闲置锁（引用计数为0） | 设备：{ProductKey}/{DeviceName}",
//                                            consumerId, eventData.MqttTrackId, partitionKey, eventData.ProductKey, eventData.DeviceName);
//                                    }
//                                    catch (Exception ex)
//                                    {
//                                        _logger.LogDebug(ex, "[消息消费][Consumer:{ConsumerId}] [锁] | [TrackId:{TrackId}] 清理设备[{PartitionKey}]锁时异常 | 设备：{ProductKey}/{DeviceName}",
//                                            consumerId, eventData.MqttTrackId, partitionKey, eventData.ProductKey, eventData.DeviceName);
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    #endregion
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // 消费者正常停止：服务关闭时的预期行为
//            _logger.LogInformation("[消息消费] 异常 | 消费者{ConsumerId}正常停止 | 累计无法处理消息数：{UnprocessedCount}",
//                consumerId,
//                Interlocked.Read(ref _unprocessedMessageCount));
//        }
//        catch (Exception ex)
//        {
//            // 消费者异常退出：生产环境需告警（如接入Prometheus/Grafana）
//            _logger.LogError(ex, "[消息消费] 异常 | 消费者{ConsumerId}异常退出 | 累计无法处理消息数：{UnprocessedCount}",
//                consumerId,
//                Interlocked.Read(ref _unprocessedMessageCount));
//        }
//    }
//    #endregion

//    #region 辅助方法
//    /// <summary>
//    /// 获取Payload摘要（避免日志过大，同时保留关键信息）
//    /// </summary>
//    /// <param name="payload">原始Payload</param>
//    /// <returns>Payload摘要</returns>
//    private string GetPayloadSummary(byte[] payload)
//    {
//        if (payload == null || payload.Length == 0)
//        {
//            return "空Payload";
//        }

//        try
//        {
//            // 转换为字符串，最多显示前100个字符
//            var payloadStr = System.Text.Encoding.UTF8.GetString(payload);
//            return payloadStr.Length > 100 ? $"{payloadStr[..100]}..." : payloadStr;
//        }
//        catch
//        {
//            // 非UTF8编码，显示字节长度
//            return $"非UTF8编码，字节长度：{payload.Length}";
//        }
//    }
//    #endregion

//    #region 资源释放 （生产级优雅关闭）
//    /// <summary>
//    /// 优雅释放调度相关资源（无业务资源）
//    /// 释放资源（单例模式必须实现，避免内存/线程泄漏）
//    /// 设计思路：
//    /// 1. 线程安全：加锁防止多次释放
//    /// 2. 优雅关闭：先取消令牌→完成Channel→清理锁→释放令牌源
//    /// </summary>
//    public void Dispose()
//    {
//        // 加锁保证线程安全：单例下Dispose可能被多线程调用
//        lock (_disposeLock)
//        {
//            // 已释放则直接返回，避免重复操作
//            if (_disposed)
//            {
//                _logger.LogDebug("MQTT消息处理器[{Name}] | 资源已释放，跳过重复释放 | 累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    Interlocked.Read(ref _unprocessedMessageCount));
//                return;
//            }

//            try
//            {
//                _logger.LogInformation("MQTT消息处理器[{Name}] | 开始释放资源 | 累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    Interlocked.Read(ref _unprocessedMessageCount));

//                // 步骤1：先标记为已释放（锁内操作），阻止新锁创建/获取
//                lock (_lockDictionarySync)
//                {
//                    _disposed = true;
//                }

//                // 步骤2：取消消费者，停止接收新消息
//                _cancellationTokenSource.Cancel();

//                // 步骤3：完成Channel，让消费者处理完剩余消息
//                _messageChannel.Writer.Complete();

//                // 步骤4：原子化清理所有锁（避免与Release竞态）
//                lock (_lockDictionarySync)
//                {
//                    foreach (var (_, semaphoreWithCount) in _deviceLocks)
//                    {
//                        try
//                        {
//                            semaphoreWithCount.Semaphore.Dispose(); // 捕获Dispose异常，避免批量清理失败
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogDebug(ex, "释放设备锁时异常");
//                        }
//                    }
//                    _deviceLocks.Clear();
//                }

//                // 步骤5：释放取消令牌源
//                _cancellationTokenSource.Dispose();

//                _logger.LogInformation("MQTT消息处理器[{Name}] | 资源释放完成 | 最终累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    Interlocked.Read(ref _unprocessedMessageCount));
//            }
//            catch (Exception ex)
//            {
//                // 释放异常：记录日志，但不抛出（避免影响服务关闭）
//                _logger.LogError(ex, "MQTT消息处理器[{Name}] | 释放资源时发生异常 | 累计无法处理消息数：{UnprocessedCount}",
//                    nameof(MqttClientPublishTopicEventHandler_Backup),
//                    Interlocked.Read(ref _unprocessedMessageCount));
//            }
//        }
//    }
//    #endregion
//}