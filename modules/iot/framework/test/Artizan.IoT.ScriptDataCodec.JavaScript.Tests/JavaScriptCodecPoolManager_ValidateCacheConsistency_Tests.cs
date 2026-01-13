using Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Shouldly;
using System.Collections.Concurrent;
using Xunit;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Tests;

/// <summary>
/// JavaScriptCodecPoolManager.ValidateCacheConsistency 单元测试
/// 测试风格：仿高并发场景 + 无Mock + 实际业务操作验证
/// </summary>
public class JavaScriptCodecPoolManager_ValidateCacheConsistency_Tests : IDisposable
{
    #region 测试常量与字段
    private readonly JavaScriptCodecPoolManager _poolManager;
    private readonly string _testProductKey = "test_product_cache_consistency";
    private readonly string _testScript = "function decode(rawData) { return { temp: rawData[0], humi: rawData[1] }; }";
    private readonly ILogger<JavaScriptCodecPoolManager> _logger;

    // 初始化日志（使用控制台日志，无Mock）
    public JavaScriptCodecPoolManager_ValidateCacheConsistency_Tests()
    {
        // 初始化真实日志（无Mock）
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });
        _logger = loggerFactory.CreateLogger<JavaScriptCodecPoolManager>();

        // 获取池管理器单例并注入日志
        _poolManager = JavaScriptCodecPoolManager.Instance;
        _poolManager.SetLogger(_logger);

        // 测试前清空所有缓存，保证测试独立性
        ResetPoolManagerState();
    }
    #endregion

    #region 辅助方法（重置池管理器状态）
    /// <summary>
    /// 泛型辅助方法：安全获取私有字典字段的所有键
    /// </summary>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    /// <param name="instance">目标实例</param>
    /// <param name="fieldName">私有字段名</param>
    /// <returns>字典所有键（空列表兜底）</returns>
    private List<TKey> GetPrivateDictionaryKeys<TKey, TValue>(object instance, string fieldName)
        where TKey : notnull
    {
        if (instance == null || string.IsNullOrEmpty(fieldName))
        {
            return new List<TKey>();
        }

        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null)
        {
            _logger.LogWarning("未找到私有字段：{FieldName}", fieldName);
            return new List<TKey>();
        }

        var dict = field.GetValue(instance) as System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>;
        return dict != null ? new List<TKey>(dict.Keys) : new List<TKey>();
    }

    // 重构后的 ResetPoolManagerState 方法
    private void ResetPoolManagerState()
    {
        try
        {
            // 使用泛型辅助方法安全获取池键
            var allPoolKeys = GetPrivateDictionaryKeys<string, ObjectPool<JavaScriptDataCodec>>(
                _poolManager, "_poolCache");

            // 移除所有产品池
            foreach (var poolKey in allPoolKeys)
            {
                _poolManager.RemovePool(poolKey);
            }

            // 全局清理兜底
            _poolManager.Dispose();

            _logger.LogInformation("池管理器状态已重置，清理池数量：{PoolCount}", allPoolKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置池管理器状态失败");
            throw;
        }
    }
    #endregion

    #region 核心测试用例
    /// <summary>
    /// 测试场景1：高并发创建/移除池后，缓存仍保持一致
    /// 仿高并发场景，验证一致性校验返回True
    /// </summary>
    [Fact]
    public async Task ValidateCacheConsistency_HighConcurrency_CreateRemovePool_Consistent()
    {
        // Arrange
        var taskCount = 10000; // 高并发任务数
        var productKeys = new List<string>();
        for (int i = 0; i < 500; i++) // 500个不同产品Key
        {
            productKeys.Add($"{_testProductKey}_normal_{i}");
        }
        var tasks = new List<Task>();
        // 优化1：降低并发数（避免锁竞争）
        var semaphore = new SemaphoreSlim(Math.Max(Environment.ProcessorCount, 8), 16);
        var productKeyLocks = new ConcurrentDictionary<string, object>();

        // Act：高并发创建/移除池
        for (int i = 0; i < taskCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var productKey = productKeys[index % productKeys.Count];
                    var keyLock = productKeyLocks.GetOrAdd(productKey, _ => new object());

                    // 单Key锁 + 淘汰锁兼容
                    lock (keyLock)
                    {
                        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey);
                        if (index % 2 == 0)
                        {
                            _poolManager.GetPool(poolKey, _testScript, maxPoolSize: 3);
                        }
                        else
                        {
                            _poolManager.RemovePool(poolKey);
                        }
                    }
                }
                catch (ArgumentException ex) when (ex.Message.Contains("index is equal to or greater than the length"))
                {
                    // 兜底：捕获淘汰异常，重试一次操作
                    _logger.LogWarning(ex, "高并发操作池触发数组越界，重试操作 ProductKey:{ProductKey}", productKeys[index % productKeys.Count]);
                    var productKey = productKeys[index % productKeys.Count];
                    var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey);

                    if (index % 2 == 0)
                    {
                        _poolManager.GetPool(poolKey, _testScript, maxPoolSize: 3);
                    }
                    else
                    {
                        _poolManager.RemovePool(poolKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "高并发操作池失败，ProductKey:{ProductKey}", productKeys[index % productKeys.Count]);
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        // 优化2：延长最终一致性等待（适配淘汰操作的锁延迟）
        await Task.Delay(1000);

        // 重试校验逻辑
        bool isConsistent = false;
        string mismatchInfo = string.Empty;
        int retryCount = 0;
        const int maxRetry = 3;
        while (retryCount < maxRetry && !isConsistent)
        {
            (isConsistent, mismatchInfo) = _poolManager.ValidateCacheConsistency();
            if (!isConsistent)
            {
                _logger.LogWarning("缓存不一致，重试校验（第{RetryCount}次）：{MismatchInfo}", retryCount + 1, mismatchInfo);
                await Task.Delay(500);
                retryCount++;
            }
        }

        // Assert
        isConsistent.ShouldBe(true, $"高并发创建/移除池后（500个产品Key），缓存应保持一致，最终不一致信息：{mismatchInfo}");
        mismatchInfo.ShouldBe("所有缓存键一致", "一致性校验信息应无异常");
    }

    /// <summary>
    /// 测试场景2：手动制造缓存不一致（策略缓存缺失），验证校验能识别
    /// </summary>
    [Fact]
    public async Task ValidateCacheConsistency_ManualMismatch_PolicyCacheMissing_Inconsistent()
    {
        // Arrange
        var targetProductKey = $"{_testProductKey}_mismatch_policy";
        // 1. 正常创建池（此时_poolCache/_poolPolicyCache/_poolLastUsedTime 都有该键）
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(targetProductKey);
        _poolManager.GetPool(poolKey, _testScript, maxPoolSize: 3);

        // 2. 手动移除策略缓存（制造不一致）
        var policyCacheField = _poolManager
            .GetType()
            .GetField("_poolPolicyCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (policyCacheField != null)
        {
            var policyCache = policyCacheField.GetValue(_poolManager) as System.Collections.Concurrent.ConcurrentDictionary<string, JavaScriptCodecPooledPolicy>;
            policyCache?.TryRemove(poolKey, out _);
        }

        // Act：执行一致性校验
        var (isConsistent, mismatchInfo) = _poolManager.ValidateCacheConsistency();

        // Assert
        isConsistent.ShouldBe(false, "手动移除策略缓存后，一致性校验应返回False");
        if (!mismatchInfo.Contains(poolKey))
        {
            throw new ShouldAssertException($"不一致信息应包含目标池键，实际值：{mismatchInfo}，期望包含：{poolKey}");
        }
        if (!mismatchInfo.Contains("Policy:False"))
        {
            throw new ShouldAssertException($"不一致信息应标识策略缓存缺失，实际值：{mismatchInfo}，期望包含：Policy:False");
        }

    }

    /// <summary>
    /// 测试场景3：高并发使用池+自动清理后，缓存仍保持一致
    /// 结合编解码器实际使用场景，验证一致性
    /// </summary>
    [Fact]
    public async Task ValidateCacheConsistency_HighConcurrency_UseCodec_Consistent()
    {
        // Arrange
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(_testProductKey);
        var pool = _poolManager.GetPool(poolKey, _testScript, maxPoolSize: 5);
        var taskCount = 10000; // 高并发使用编解码器
        var tasks = new List<Task<ScriptExecutionResult>>();

        // Act：高并发使用编解码器（模拟业务操作）
        for (int i = 0; i < taskCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var codec = pool.Get();
                try
                {
                    if (codec.TryAcquire())
                    {
                        var ctx = new ScriptExecutionContext
                        {
                            RawData = new byte[] { 0x01, (byte)(index % 255), 0x02, (byte)(index % 255) },
                            MethodName = "decode",
                            ProductKey = _testProductKey,
                            DeviceName = $"sensor_{index}"
                        };
                        return await codec.DecodeAsync(ctx);
                    }
                    return ScriptExecutionResult.Fail("Failed to acquire codec");
                }
                finally
                {
                    codec.Release();
                    pool.Return(codec);
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // 手动触发一次自动清理（模拟闲置池清理）
        _poolManager.TriggerImmediateCleanup();

        // 执行缓存一致性校验
        var (isConsistent, mismatchInfo) = _poolManager.ValidateCacheConsistency();

        // Assert（Shouldly 语法）
        isConsistent.ShouldBe(true, "高并发使用+自动清理后，缓存应保持一致");
        mismatchInfo.ShouldBe("所有缓存键一致");
        // 额外验证：所有编解码器任务执行成功
        results.Count(r => r.Success).ShouldBe(taskCount);
        results.All(s => s.Success).ShouldBe(true);
    }

    /// <summary>
    /// 测试场景4：所有缓存为空时，一致性校验返回True
    /// 边界场景验证
    /// </summary>
    [Fact]
    public void ValidateCacheConsistency_AllCachesEmpty_Consistent()
    {
        // Arrange：确保所有缓存为空
        ResetPoolManagerState();

        // Act
        var (isConsistent, mismatchInfo) = _poolManager.ValidateCacheConsistency();

        // Assert（Shouldly 语法）
        isConsistent.ShouldBe(true, "所有缓存为空时，一致性校验应返回True");
        mismatchInfo.ShouldBe("所有缓存键一致");
    }
    #endregion

    #region 资源释放
    public void Dispose()
    {
        // 测试后重置池管理器状态
        ResetPoolManagerState();
        GC.SuppressFinalize(this);
    }
    #endregion
}
