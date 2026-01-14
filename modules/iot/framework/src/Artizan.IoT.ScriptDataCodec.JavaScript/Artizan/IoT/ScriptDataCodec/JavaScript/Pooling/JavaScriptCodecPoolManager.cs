using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;

/// <summary>
/// JavaScript编解码器对象池管理器（单例模式 + 内置定时清理）
/// 设计思路：
/// 1. 单例模式：全局唯一管理器，避免重复创建对象池，降低内存开销；
/// 2. 自包含定时清理：内置StartAutoCleanupTask，无需依赖外部框架（如Abp后台任务），降低使用复杂度；
/// 3. 资源回收策略：
///    - 自动清理：定时清理超过闲置阈值的废弃池；
///    - LRU淘汰：池数量超上限时淘汰最久未使用的池；
///    - 手动清理：提供API支持产品下线时即时清理；
/// 4. 线程安全：所有字典操作使用ConcurrentDictionary，原子操作控制状态；
/// 5. 可配置化：关键参数（闲置阈值、清理间隔等）支持外部调整；
/// 6. 日志适配：支持注入ILogger，无日志依赖时使用空日志兜底；
/// 设计模式：
/// - 单例模式（懒加载）：保证全局唯一实例，避免多实例导致池管理混乱；
/// - 对象池模式：复用JavaScriptDataCodec实例，减少Jint引擎创建开销；
/// - 定时任务模式：后台线程定时执行清理，非阻塞主线程；
/// 设计考量：
/// - 内聚性：清理逻辑与池管理逻辑内聚，符合单一职责原则；
/// - 防御性编程：所有外部输入（如productKey）做合法性校验，提前拦截异常；
/// - 异常兜底：清理任务异常时记录日志并重试，避免任务终止；
/// - 资源安全：Dispose时优雅释放所有资源，避免内存泄漏。
/// </summary>
public sealed class JavaScriptCodecPoolManager : IDisposable
{
    #region 可配置参数（外部可调整，带默认值）
    /// <summary>
    /// 池闲置阈值（超过则判定为废弃，默认7天）
    /// 设计考量：IoT场景下产品下线后7天无访问可判定为废弃，可根据业务调整
    /// </summary>
    public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 自动清理执行间隔（默认24小时）
    /// 设计考量：低频清理减少性能损耗，高频清理可缩短至12小时/6小时
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// 全局池数量上限（默认100）
    /// 设计考量：避免池数量爆炸导致内存耗尽，按服务器配置调整（如200/300）
    /// </summary>
    public int MaxPoolCount { get; set; } = 100;

    /// <summary>
    /// 是否开启自动清理（默认开启）
    /// 设计考量：支持测试环境关闭自动清理，生产环境默认开启
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;
    #endregion

    #region 私有核心字段
    /// <summary>
    /// 池元数据：池键 → 最后使用时间（用于判断闲置）
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _poolLastUsedTime = new ConcurrentDictionary<string, DateTime>();

    /// <summary>
    /// 池缓存：池键 → 编解码器池实例，Key=产品池Key，Value=编解码器池
    /// </summary>
    private readonly ConcurrentDictionary<string, ObjectPool<JavaScriptDataCodec>> _poolCache = new ConcurrentDictionary<string, ObjectPool<JavaScriptDataCodec>>();

    /// <summary>
    /// 池策略缓存：池键 → 对应的编解码器池策略实例，Key=产品池Key，Value=池策略
    /// 设计思路：
    /// 1. 关联池键与自定义池策略，避免反射获取池上限（MaxSize），提升性能与稳定性；
    /// 2. 存储每个产品池的创建策略，便于后续释放资源时精准获取池容量上限；
    /// 设计模式：缓存模式（ConcurrentDictionary保证线程安全）；
    /// 设计考量：
    /// - 与_poolCache一一对应，确保每个对象池都能找到其创建时的策略；
    /// - 线程安全：使用ConcurrentDictionary适配高并发场景下的池创建/移除操作；
    /// - 内存可控：与_poolCache同步清理，避免策略缓存泄漏。
    /// </summary>
    private readonly ConcurrentDictionary<string, JavaScriptCodecPooledPolicy> _poolPolicyCache = new ConcurrentDictionary<string, JavaScriptCodecPooledPolicy>();



    /// <summary>
    /// 定时清理任务取消令牌
    /// </summary>
    private readonly CancellationTokenSource _cleanupCts = new CancellationTokenSource();

    /// <summary>
    /// 定时清理任务（后台执行）
    /// </summary>
    private Task _cleanupTask;

    /// <summary>
    /// 淘汰操作锁：保证LRU淘汰线程安全
    /// 避免：EvictLeastRecentlyUsedPools 中遍历动态字典时未加锁，导致数组拷贝长度不匹配；
    /// </summary>
    private readonly object _evictLock = new object(); // 淘汰操作锁

    /// <summary>
    /// 日志实例（支持注入，兜底空日志）
    /// </summary>
    private ILogger<JavaScriptCodecPoolManager> _logger;
    private CodecLogger _codecLogger; // 全局复用

    #endregion

    #region 单例实现（懒加载 + 线程安全）
    /// <summary>
    /// 懒加载单例（保证线程安全，首次访问时初始化）
    /// </summary>
    private static readonly Lazy<JavaScriptCodecPoolManager> _instance =
            new Lazy<JavaScriptCodecPoolManager>(
                () => new JavaScriptCodecPoolManager(),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

    /// <summary>
    /// 私有构造函数（禁止外部实例化，初始化定时任务）
    /// </summary>
    private JavaScriptCodecPoolManager()
    {
        // 初始化空日志兜底
        _logger = NullLogger<JavaScriptCodecPoolManager>.Instance;
        _codecLogger = new CodecLogger(_logger);

        // 启动内置定时清理任务
        StartAutoCleanupTask();
    }

    /// <summary>
    /// 全局单例实例
    /// </summary>
    public static JavaScriptCodecPoolManager Instance => _instance.Value;
    #endregion

    #region 日志注入（外部扩展）
    /// <summary>
    /// 注入日志实例（可选，推荐生产环境调用）
    /// 设计考量：支持主流日志框架（Serilog/Log4Net等），无侵入式扩展
    /// </summary>
    /// <param name="logger">日志实例</param>
    public void SetLogger(ILogger<JavaScriptCodecPoolManager> logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger), "日志器不能为空");
        }

        // 直接复用已有的 _evictLock 锁（无需新增锁），既保证 SetLogger 线程安全，又避免锁膨胀（过多锁增加复杂度）。
        lock (_evictLock) // 加锁保证线程安全
        {
            _logger = logger;
            // 初始化全局CodecLogger（复用，仅初始化一次）
            _codecLogger = new CodecLogger(logger);
        }
    }
    #endregion

    #region 核心API：获取/移除对象池
    /// <summary>
    /// 获取指定产品的编解码器对象池
    /// 设计思路：
    /// 1. 生成标准化池键，避免键冲突；
    /// 2. 容量超限触发LRU淘汰，防止内存爆炸；
    /// 3. 复用已有池，无则创建新池；
    /// 4. 更新最后使用时间，标记为活跃池。
    /// </summary>
    /// <param name="poolKey">标准化池键</param>
    /// <param name="scriptContent">产品专属JS编解码脚本</param>
    /// <param name="maxPoolSize">单个产品池的最大实例数（默认CPU核心数×2）</param>
    /// <returns>产品专属的对象池实例</returns>
    /// <exception cref="ArgumentNullException">productKey/scriptContent为空时抛出</exception>
    /// <exception cref="ArgumentException">productKey含非法字符时抛出</exception>
    public ObjectPool<JavaScriptDataCodec> GetPool(string poolKey, string scriptContent, int? maxPoolSize = null)
    {
        var productKey = JavaScriptCodecPoolKeyHelper.GetProductKeyFromPoolKey(poolKey);

        try
        {
            // 空值校验：脚本内容不能为空（编解码器核心依赖）
            if (string.IsNullOrEmpty(scriptContent))
            {
                throw new ArgumentNullException(nameof(scriptContent), "JS编解码脚本内容不能为空");
            }

            // 容量控制：超过上限时淘汰LRU池（保留90%容量，避免频繁淘汰）
            if (_poolCache.Count >= MaxPoolCount)
            {
                _logger.LogWarning("对象池总数达到上限[{MaxPoolCount}]，触发LRU淘汰", MaxPoolCount);
                EvictLeastRecentlyUsedPools();
            }

            // 4. 获取/创建对象池（线程安全）
            var pool = _poolCache.GetOrAdd(poolKey, key =>
            {
                var size = maxPoolSize ?? Environment.ProcessorCount * 2;
                var policy = new JavaScriptCodecPooledPolicy(scriptContent, _codecLogger, size);
                _poolPolicyCache.GetOrAdd(key, policy);
                _logger.LogInformation("创建产品[{ProductKey}]的编解码器对象池，池容量[{PoolSize}]", productKey, size);
                return new DefaultObjectPool<JavaScriptDataCodec>(policy, size);
            });

            // 5. 更新最后使用时间（标记为活跃池，避免被清理）
            _poolLastUsedTime[poolKey] = DateTime.Now;

            return pool;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取产品[{ProductKey}]的对象池失败", productKey);
            throw; // 抛出异常，让上层感知并处理
        }
    }

    /// <summary>
    /// 手动移除指定产品的对象池（产品下线/脚本更新时调用）
    /// 设计思路：
    /// 1. 移除池元数据和缓存；
    /// 2. 释放池内所有编解码器实例，避免内存泄漏；
    /// 3. 日志记录清理结果，便于运维排查。
    /// </summary>
    /// <param name="poolKey">池键</param>
    /// <param name="releaseResources">是否释放池内所有实例（默认true）</param>
    /// <returns>是否移除成功</returns>
    public bool RemovePool(string poolKey, bool releaseResources = true)
    {
        var productKey = JavaScriptCodecPoolKeyHelper.GetProductKeyFromPoolKey(poolKey);

        try
        {
            // 原子操作：先移除池，再移除策略和时间缓存（避免高并发下的残留）
            if (_poolCache.TryRemove(poolKey, out var pool))
            {
                // 1. 释放池资源
                if (releaseResources)
                {
                    ReleasePoolResources(pool, poolKey);
                }

                // 2. 原子移除策略缓存（TryRemove保证线程安全）
                _poolPolicyCache.TryRemove(poolKey, out _);
                // 3. 原子移除时间缓存
                _poolLastUsedTime.TryRemove(poolKey, out _);

                _logger.LogInformation("手动清理产品[{ProductKey}]的对象池成功", productKey);
                return true;
            }

            _logger.LogWarning("手动清理产品[{ProductKey}]的对象池失败：池不存在", productKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动清理产品[{ProductKey}]的对象池失败", productKey);
            return false;
        }
    }
    #endregion

    #region 辅助API：手动触发清理/获取统计信息
    /// <summary>
    /// 手动触发一次全量清理（紧急清理时调用，如内存告警）
    /// </summary>
    public void TriggerImmediateCleanup()
    {
        try
        {
            _logger.LogInformation("手动触发对象池全量清理");
            CleanupIdlePools();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动触发对象池清理失败");
        }
    }

    /// <summary>
    /// 获取对象池统计信息（监控/调试用）
    /// 设计考量：暴露关键指标，支持监控告警（如闲置池过多、内存占用过高）
    /// </summary>
    /// <returns>总池数、闲置池数、估算占用内存（MB）</returns>
    public (int TotalPoolCount, int IdlePoolCount, long EstimatedMemoryMB) GetPoolStats()
    {
        try
        {
            var now = DateTime.Now;
            // 计算闲置池数量（超过IdleThreshold未使用）
            var idleCount = _poolLastUsedTime.Count(kv => now - kv.Value > IdleThreshold);
            // 估算内存：每个池平均占用2MB × 池内实例数（默认CPU核心数×2）
            var estimatedMemory = _poolCache.Count * Environment.ProcessorCount * 2 * 2;

            _logger.LogInformation("对象池统计：总池数[{Total}]，闲置池数[{Idle}]，估算内存[{Memory}]MB",
                _poolCache.Count, idleCount, estimatedMemory);

            return (_poolCache.Count, idleCount, estimatedMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对象池统计信息失败");
            return (0, 0, 0); // 异常时返回默认值，避免上层崩溃
        }
    }
    #endregion

    #region 监控/调试用

    /// <summary>
    /// 校验缓存一致性（监控/调试用）
    /// 设计思路：确保_poolCache/_poolPolicyCache/_poolLastUsedTime的键完全一致
    /// </summary>
    /// <returns>是否一致 + 不一致的键信息</returns>
    public (bool IsConsistent, string MismatchInfo) ValidateCacheConsistency()
    {
        var poolKeys = new HashSet<string>(_poolCache.Keys);
        var policyKeys = new HashSet<string>(_poolPolicyCache.Keys);
        var timeKeys = new HashSet<string>(_poolLastUsedTime.Keys);

        // 检查三者键是否完全一致
        var allKeys = new HashSet<string>(poolKeys);
        allKeys.UnionWith(policyKeys);
        allKeys.UnionWith(timeKeys);

        var mismatchKeys = new List<string>();
        foreach (var key in allKeys)
        {
            var inPool = poolKeys.Contains(key);
            var inPolicy = policyKeys.Contains(key);
            var inTime = timeKeys.Contains(key);

            if (inPool != inPolicy || inPool != inTime)
            {
                mismatchKeys.Add($"{key} - Pool:{inPool}, Policy:{inPolicy}, Time:{inTime}");
            }
        }

        var isConsistent = mismatchKeys.Count == 0;
        var mismatchInfo = isConsistent ? "所有缓存键一致" : string.Join("; ", mismatchKeys);

        _logger.LogInformation("缓存一致性校验：{IsConsistent} | 不一致信息：{MismatchInfo}", isConsistent, mismatchInfo);
        return (isConsistent, mismatchInfo);
    }

    #endregion

    #region 核心逻辑：定时清理/LRU淘汰/资源释放
    /// <summary>
    /// 启动内置定时清理任务（自包含，无需外部依赖）
    /// 设计思路：
    /// 1. 后台线程执行，非阻塞主线程；
    /// 2. 异常时记录日志并重试，避免任务终止；
    /// 3. 支持取消令牌，Dispose时优雅停止。
    /// </summary>
    private void StartAutoCleanupTask()
    {
        if (!EnableAutoCleanup)
        {
            _logger.LogInformation("对象池自动清理已禁用，跳过任务启动");
            return;
        }

        _cleanupTask = Task.Run(async () =>
        {
            _logger.LogInformation("对象池自动清理任务启动，清理间隔[{Interval}]，闲置阈值[{Threshold}]",
                CleanupInterval, IdleThreshold);

            while (!_cleanupCts.Token.IsCancellationRequested)
            {
                try
                {
                    // 执行清理逻辑
                    CleanupIdlePools();

                    // 等待下一次执行（支持取消）
                    await Task.Delay(CleanupInterval, _cleanupCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 任务正常取消（Dispose时），不记录异常
                    _logger.LogInformation("对象池自动清理任务已取消");
                    break;
                }
                catch (Exception ex)
                {
                    // 异常时记录日志，5分钟后重试，避免任务终止
                    _logger.LogError(ex, "对象池自动清理任务执行失败，5分钟后重试");
                    await Task.Delay(TimeSpan.FromMinutes(5), _cleanupCts.Token);
                }
            }
        }, _cleanupCts.Token);
    }

    /// <summary>
    /// 清理闲置超过阈值的池
    /// </summary>
    private void CleanupIdlePools()
    {
        var now = DateTime.Now;
        // 筛选出闲置池
        var idlePoolKeys = _poolLastUsedTime
            .Where(kv => now - kv.Value > IdleThreshold)
            .Select(kv => kv.Key)
            .ToList();

        if (idlePoolKeys.Count == 0)
        {
            _logger.LogInformation("无闲置对象池需要清理");
            return;
        }

        // 批量清理闲置池
        var cleanedCount = 0;
        foreach (var poolKey in idlePoolKeys)
        {
            if (_poolCache.TryRemove(poolKey, out var pool))
            {
                // 释放池内资源
                ReleasePoolResources(pool, poolKey);
                // 移除池策略缓存
                _poolPolicyCache.TryRemove(poolKey, out _);
                // 移除元数据
                _poolLastUsedTime.TryRemove(poolKey, out _);
                cleanedCount++;

                // 解析产品Key，便于日志展示
                var productKey = JavaScriptCodecPoolKeyHelper.GetProductKeyFromPoolKey(poolKey);
                _logger.LogInformation("自动清理闲置产品[{ProductKey}]的对象池", productKey);
            }
        }

        _logger.LogInformation("对象池自动清理完成 | 清理数量[{CleanedCount}] | 剩余池总数[{RemainingCount}]",
            cleanedCount, _poolCache.Count);
    }

    /// <summary>
    /// 淘汰最久未使用的池（LRU算法）
    /// 触发条件：对象池总数超出 MaxPoolCount 上限时触发
    /// 设计思路：
    /// 1. 基于快照排序：先拷贝字典为数组再排序，避免高并发下集合长度变化导致的数组越界；
    /// 2. 精准淘汰：仅淘汰超出 MaxPoolCount 上限的部分，避免过度淘汰/频繁淘汰；
    /// 3. 线程安全：淘汰过程加锁，确保高并发下字典操作的原子性；
    /// 4. 按最后使用时间排序：优先淘汰最久未使用的池，保证活跃池的可用性。
    /// </summary>
    private void EvictLeastRecentlyUsedPools()
    {
        /* --------------------------------------------------------------------------------------------------------------------------------------------
           高并发下 _poolLastUsedTime（存储池最后使用时间的字典）的元素数量在「获取长度」和「拷贝数组」之间发生了变化（被其他线程删除 / 添加），导致：
           先获取字典长度 count = _poolLastUsedTime.Count，创建了长度为 count 的数组；
           但拷贝时字典元素数已变化（> count），触发 ArgumentException（索引超出数组长度）。
           
           核心：EvictLeastRecentlyUsedPools 方法线程安全改造
           需给「获取字典快照 → 排序 → 淘汰」的核心逻辑加锁，避免高并发下集合长度变化。
         */

        // 加锁：确保淘汰过程中字典不被并发修改，避免数组拷贝越界
        lock (_evictLock)
        {
            try
            {
                // 步骤1：获取最后使用时间字典的快照（ToArray），避免遍历中字典变化
                var sortedPools = _poolLastUsedTime
                    .ToArray() // 快照拷贝，解决高并发下Count不一致问题
                    .OrderBy(kv => kv.Value) // 按最后使用时间升序，最久未使用的排在前面
                    .ToList();

                // 步骤2：计算需要淘汰的数量（仅淘汰超出上限的部分）
                // 核心逻辑：只淘汰超出MaxPoolCount的池，避免提前淘汰导致的性能损耗
                int evictCount = sortedPools.Count - MaxPoolCount;
                if (evictCount <= 0)
                {
                    _logger.LogDebug("对象池数量未超限（当前：{CurrentCount}，上限：{MaxCount}），无需淘汰",
                        sortedPools.Count, MaxPoolCount);
                    return;
                }

                // 步骤3：执行LRU淘汰（仅淘汰超出部分）
                _logger.LogWarning("对象池数量超限（当前：{CurrentCount}，上限：{MaxCount}），触发LRU淘汰，需淘汰{EvictCount}个池",
                    sortedPools.Count, MaxPoolCount, evictCount);

                for (int i = 0; i < evictCount; i++)
                {
                    var poolKey = sortedPools[i].Key;
                    var productKey = JavaScriptCodecPoolKeyHelper.GetProductKeyFromPoolKey(poolKey);

                    RemovePool(poolKey); // 移除池并清理所有关联缓存
                    _logger.LogInformation("LRU淘汰完成：移除最久未使用的产品池 {ProductKey}", productKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LRU淘汰池失败");
                // 避免淘汰失败导致整体流程中断
                throw; // 抛出异常，确保异常可感知（测试/生产环境可根据需求调整为吞异常）
            }
        }
    }

    /// <summary>
    /// 释放池内所有编解码器实例资源
    /// 设计思路：
    /// 1. 从自定义池策略中获取池上限，避免反射和死循环；
    /// 2. 按池上限次数获取实例，确保覆盖所有池内实例；
    /// 3. 释放实例后不归还到池，防止复用已释放的资源；
    /// 4. 异常兜底，避免影响主线程。
    /// 设计考量：兼容DefaultObjectPool特性，兼顾性能与安全性。
    /// </summary>
    /// <param name="pool">对象池实例</param>
    /// <param name="poolKey">池唯一标识</param>
    private void ReleasePoolResources(ObjectPool<JavaScriptDataCodec> pool, string poolKey)
    {
        // 空值校验
        if (pool == null || string.IsNullOrEmpty(poolKey))
        {
            return;
        }

        try
        {
            // 从缓存获取池策略（含MaxSize）
            if (!_poolPolicyCache.TryGetValue(poolKey, out var policy))
            {
                _logger.LogWarning("未找到池[{PoolKey}]的策略，无法释放资源", poolKey);
                return;
            }

            // 按池上限循环释放实例（最多policy.MaxSize次）
            int releasedCount = 0;
            for (int i = 0; i < policy.MaxSize; i++)
            {
                var codec = pool.Get();
                if (codec == null)
                {
                    break;
                }

                // 释放实例资源
                codec.Dispose();
                releasedCount++;

                // 【核心】不归还实例到池，避免池重新使用已释放的实例
                //  这里移除 pool.Return(codec)，避免池重新持有已释放的实例
            }

            _logger.LogInformation("池[{PoolKey}]资源释放完成，释放实例数[{ReleasedCount}]/[{MaxSize}]",
                poolKey, releasedCount, policy.MaxSize);
        }
        catch (Exception ex)
        {
            // 捕获所有异常，避免影响主线程
            _logger.LogWarning(ex, "池[{PoolKey}]资源释放失败", poolKey);
        }
    }
    #endregion

    #region 资源释放：Dispose（优雅关闭）
    /// <summary>
    /// 释放所有资源（程序退出/服务停止时调用）
    /// 设计思路：
    /// 1. 取消定时任务；
    /// 2. 清理所有对象池；
    /// 3. 释放取消令牌；
    /// 4. 日志记录释放结果。
    /// </summary>
    public void Dispose()
    {
        try
        {
            // 1. 取消定时清理任务
            _cleanupCts.Cancel();
            if (_cleanupTask != null && !_cleanupTask.IsCompleted)
            {
                _cleanupTask.Wait(TimeSpan.FromSeconds(5)); // 等待5秒，避免强制终止
            }

            // 2. 清理所有对象池
            var poolCount = _poolCache.Count;
            foreach (var poolKey in _poolCache.Keys.ToList())
            {
                RemovePool(poolKey);
            }

            // 3. 清空所有缓存, 避免内存泄漏
            // 每个池的「对象池 + 策略缓存 + 使用时间」三者清理保持原子性，避免部分清理导致的键值对不一致；
            _poolCache.Clear();
            _poolPolicyCache.Clear(); // 清空策略缓存
            _poolLastUsedTime.Clear();

            // 4. 释放取消令牌
            _cleanupCts.Dispose();

            _logger.LogInformation("对象池管理器已释放所有资源 | 清理池总数[{PoolCount}]", poolCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "对象池管理器释放资源失败");
        }
    }
    #endregion

}
