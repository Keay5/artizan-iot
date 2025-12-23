using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Concurrents
{
    /// <summary>
    /// 泛型高并发分区调度器实现
    /// 【整体设计理念】：以「分区隔离+精准并发控制」为核心，平衡高吞吐与任务有序性，适配多场景高并发任务调度
    /// 【核心设计逻辑】：
    /// 1. 分区隔离：按partitionKey哈希映射固定分区，实现同一业务主体（如同一设备）任务串行执行，避免数据乱序/覆盖；不同分区并行消费，提升整体吞吐
    /// 2. 并发可控：基于信号量控制单分区内任务并发度，适配IO/CPU密集型任务差异，防止单分区过载
    /// 3. 高效消费：任务入队即触发消费，消费完毕自动释放状态，结合预消费机制减少等待开销，无空轮询浪费
    /// 4. 异常防护：任务超时控制+单个任务失败隔离，避免异常任务阻塞整个分区，提升系统稳定性
    /// 5. 配置化扩展：分区数支持配置动态调整，适配不同业务规模（设备/用户量），弹性支撑高并发扩展
    /// </summary>
    /// <typeparam name="T">任务执行结果类型，与接口泛型参数一致，适配任意业务结果</typeparam>
    public abstract class DefaultHighConcurrencyPartitionScheduler<T> :
        IHighConcurrencyPartitionScheduler<T>,
        ISingletonDependency // 全局唯一实例，避免重复初始化分区组件，节省资源
    {
        /// <summary>
        /// 高并发分区调度器配置选项类
        /// </summary>
        private readonly PartitionSchedulerOptions _schedulerOptions;

        /// <summary>
        /// 分区任务队列数组（每个分区独立队列，物理隔离任务）
        /// 【设计思路】：采用ConcurrentQueue（线程安全无锁队列），支持多线程并发入队/出队，性能高效；数组索引对应分区索引，映射关系清晰
        /// </summary>
        private readonly ConcurrentQueue<Func<Task>>[] _partitionQueues;

        /// <summary>
        /// 分区信号量数组（控制单个分区内任务并发度）
        /// 【设计思路】：每个分区绑定独立信号量，精准控制该分区同时执行的任务数，避免单分区任务堆积或资源争抢
        /// </summary>
        private readonly SemaphoreSlim[] _partitionSemaphores;

        /// <summary>
        /// 分区消费状态数组（标记分区是否正在消费任务）
        /// 【设计思路】：防止多线程重复触发同一分区消费逻辑，减少无效线程创建，降低资源开销
        /// </summary>
        private readonly bool[] _isPartitionConsuming;

        /// <summary>
        /// 构造函数（初始化分区核心组件，读取配置参数）
        /// 【初始化逻辑】：依赖配置服务读取分区数，批量初始化各分区的队列、信号量、消费状态，确保组件就绪
        /// </summary>
        /// <param name="configuration">配置服务（ABP自动注入，读取appsettings.json配置）</param>
        public DefaultHighConcurrencyPartitionScheduler(IOptions<PartitionSchedulerOptions> schedulerOptions)
        {
            // 从Options读取配置参数，覆盖默认值
            _schedulerOptions = schedulerOptions.Value;

            var partitionCount = _schedulerOptions.PartitionCount;
            var maxParallelPerPartition = _schedulerOptions.MaxParallelPerPartition;
            var taskTimeout = _schedulerOptions.TaskTimeout;

            // 初始化分区队列、信号量、消费状态数组（长度=分区数，一一对应）
            _partitionQueues = new ConcurrentQueue<Func<Task>>[partitionCount];
            _partitionSemaphores = new SemaphoreSlim[partitionCount];
            _isPartitionConsuming = new bool[partitionCount];

            // 循环初始化每个分区的核心组件
            for (int i = 0; i < partitionCount; i++)
            {
                _partitionQueues[i] = new ConcurrentQueue<Func<Task>>(); // 每个分区独立线程安全队列
                // 信号量初始化：初始可用数=最大并发数，上限=最大并发数，精准控制分区并发
                _partitionSemaphores[i] = new SemaphoreSlim(maxParallelPerPartition, maxParallelPerPartition);
                _isPartitionConsuming[i] = false; // 初始标记分区未消费，空闲状态
            }
        }

        /// <summary>
        /// 提交泛型任务到对应分区执行（核心调度方法）
        /// 【调度全流程】：参数校验→分区路由→任务包装（超时+结果回调）→任务入队→触发消费→等待结果返回
        /// </summary>
        /// <param name="partitionKey">分区键（路由分区的唯一依据）</param>
        /// <param name="taskFunc">业务异步任务委托（由业务层传入，包含具体业务逻辑）</param>
        /// <returns>业务任务执行后的T类型结果</returns>
        /// <exception cref="ArgumentNullException">分区键或任务委托为空时抛出，提前拦截无效参数</exception>
        public async Task<T> ScheduleAsync(string partitionKey, Func<Task<T>> taskFunc)
        {
            // 参数校验：提前拦截空值，避免后续逻辑异常，提升容错性
            if (string.IsNullOrEmpty(partitionKey))
            {
                throw new ArgumentNullException(nameof(partitionKey), "分区键不能为空，需传入有效业务标识（如设备ID、用户ID）");
            }
            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc), "任务委托不能为空，需封装具体业务异步逻辑");
            }

            // 1. 分区路由：根据分区键哈希计算对应分区索引，保证同一分区键始终映射同一分区
            int partitionIndex = GetPartitionIndex(partitionKey);
            // 获取当前分区的任务队列和信号量（分区内组件绑定，隔离控制）
            var taskQueue = _partitionQueues[partitionIndex];
            var semaphore = _partitionSemaphores[partitionIndex];

            // 2. 任务包装：通过TaskCompletionSource实现任务结果回调，封装超时控制与异常捕获，隔离业务与调度异常
            var taskCompletionSource = new TaskCompletionSource<T>();
            Func<Task> wrappedTask = async () =>
            {
                await semaphore.WaitAsync(); // 信号量等待：获取分区并发许可，无许可则阻塞，控制分区内并发数
                try
                {
                    // 任务执行+超时控制：通过WaitAsync设置超时，超时直接抛出异常，避免阻塞分区
                    var result = await Task.Run(taskFunc).WaitAsync(_schedulerOptions.TaskTimeout);
                    taskCompletionSource.SetResult(result); // 任务成功，设置结果供外部等待获取
                }
                catch (TimeoutException ex)
                {
                    // 超时异常处理：封装超时信息，明确异常原因，便于排查
                    taskCompletionSource.SetException(new Exception($"任务执行超时（超时阈值：{_schedulerOptions.TaskTimeout.TotalMilliseconds}ms），分区索引：{partitionIndex}", ex));
                }
                catch (Exception ex)
                {
                    // 通用异常处理：捕获业务任务所有异常，避免单个任务异常扩散
                    taskCompletionSource.SetException(new Exception($"分区[{partitionIndex}]任务执行失败", ex));
                }
                finally
                {
                    semaphore.Release(); // 释放信号量：无论任务成功失败，均释放许可，避免信号量泄漏
                    TriggerNextTaskConsume(partitionIndex); // 预消费下一个任务：任务执行完毕立即触发后续消费，减少等待，提升吞吐
                }
            };

            // 3. 任务入队+触发消费：任务入队后立即触发消费逻辑，确保任务快速执行
            taskQueue.Enqueue(wrappedTask);
            TriggerNextTaskConsume(partitionIndex);

            // 4. 等待任务结果：返回TaskCompletionSource的任务，供外部异步等待，获取业务结果
            return await taskCompletionSource.Task;
        }

        /// <summary>
        /// 触发指定分区的任务消费（核心消费驱动方法）
        /// 【消费逻辑】：双重校验锁控制消费状态，单分区单消费线程循环出队执行任务，队列空则释放状态，避免资源浪费
        /// </summary>
        /// <param name="partitionIndex">分区索引（指定要消费的分区）</param>
        private void TriggerNextTaskConsume(int partitionIndex)
        {
            // 第一重校验：快速判断分区是否正在消费，若正在消费直接返回，避免进入锁逻辑，提升性能
            if (_isPartitionConsuming[partitionIndex])
            {
                return;
            }

            // 双重校验锁：锁定泛型类类型（不同泛型实例类型不同，避免跨泛型实例锁冲突），确保线程安全
            lock (typeof(DefaultHighConcurrencyPartitionScheduler<T>))
            {
                // 第二重校验：进入锁后再次判断，防止多线程并发触发时重复标记消费状态
                if (_isPartitionConsuming[partitionIndex])
                {
                    return;
                }
                _isPartitionConsuming[partitionIndex] = true; // 标记分区正在消费，禁止其他线程重复触发
            }

            // 后台异步消费：通过Task.Run开启后台线程消费，不阻塞任务提交线程，实现异步解耦
            _ = Task.Run(async () =>
            {
                var taskQueue = _partitionQueues[partitionIndex];
                // 循环出队执行：队列非空则持续获取任务执行，空则退出循环
                while (taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        await task(); // 执行包装后的任务（包含信号量控制、异常处理）
                    }
                    catch (Exception ex)
                    {
                        // 消费异常隔离：单个任务执行失败不中断整个分区消费，仅日志记录，提升系统鲁棒性
                        Console.WriteLine($"分区[{partitionIndex}]任务消费异常：{ex.Message}，堆栈信息：{ex.StackTrace}");
                    }
                }

                // 队列消费完毕：标记分区空闲，允许后续任务入队后重新触发消费
                _isPartitionConsuming[partitionIndex] = false;
            });
        }

        /// <summary>
        /// 分区键哈希计算分区索引（核心路由方法）
        /// 【路由设计】：采用.NET内置高效哈希算法，低冲突率；取绝对值+取模确保索引在分区范围内，映射均匀
        /// </summary>
        /// <param name="partitionKey">分区键（业务标识）</param>
        /// <returns>分区索引（0~_schedulerOptions.PartitionCount-1，确保落在分区数组范围内）</returns>
        private int GetPartitionIndex(string partitionKey)
        {
            // 哈希计算：使用HashCode.Combine，比传统GetHashCode性能更优，冲突率更低，适配字符串类型分区键
            int hashCode = HashCode.Combine(partitionKey);
            // 索引校准：取绝对值避免负索引，取模确保索引在分区总数范围内，保证映射有效性
            return Math.Abs(hashCode) % _schedulerOptions.PartitionCount;
        }

        /// <summary>
        /// 获取各分区队列状态（监控运维方法）
        /// 【设计目的】：实时统计每个分区的待执行任务数，便于监控任务堆积情况，及时调整分区数或并发参数
        /// </summary>
        /// <returns>分区索引-队列长度字典，支撑运维监控展示</returns>
        public Dictionary<int, int> GetPartitionQueueStats()
        {
            var queueStats = new Dictionary<int, int>();
            // 循环遍历所有分区，统计每个队列的任务数，存入字典返回
            for (int i = 0; i < _schedulerOptions.PartitionCount; i++)
            {
                queueStats.Add(i, _partitionQueues[i].Count);
            }
            return queueStats;
        }
    }
}
