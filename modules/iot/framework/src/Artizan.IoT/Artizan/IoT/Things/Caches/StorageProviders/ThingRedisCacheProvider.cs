using Artizan.IoT.Things.Caches.Enums;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Things.Caches.StorageProviders;

/// <summary>
/// Redis分布式缓存提供者（策略模式具体实现）
/// 设计思路：
/// 1. 基于StackExchange.Redis实现分布式缓存，支持多实例共享缓存数据
/// 2. 最新数据用String类型存储（K-V），历史数据用ZSet类型存储（时序排序）
/// 设计理念：
/// - 分布式一致性：Redis集群保证多服务实例缓存数据一致，适配生产环境部署
/// - 时序特性：ZSet的Score天然适配时间戳排序，完美支持时间范围查询
/// 设计考量：
/// - 序列化：使用System.Text.Json实现对象序列化，避免第三方依赖（如Newtonsoft.Json）
/// - 批量操作：Redis Pipeline批量执行命令，减少网络往返次数，提升吞吐量
/// - 异常处理：封装Redis操作的通用异常捕获，保证调用方稳定性
/// - 类型转换：处理TimeSpan?到Expiration的类型适配，解决CS1503编译错误
/// </summary>
public class ThingRedisCacheProvider : IThingCacheStorageProvider
{
    private readonly IDatabase _redisDb;
    private readonly ConnectionMultiplexer _redisConnection;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 构造函数（依赖注入Redis连接）
    /// </summary>
    /// <param name="redisConnection">Redis连接复用器（单例，避免频繁创建连接）</param>
    /// <param name="databaseIndex">Redis数据库索引（适配多业务隔离）</param>
    /// <exception cref="ArgumentNullException">Redis连接为空时抛出</exception>
    public ThingRedisCacheProvider(ConnectionMultiplexer redisConnection, int databaseIndex = 0)
    {
        _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        _redisDb = _redisConnection.GetDatabase(databaseIndex);
    }

    #region 最新数据操作（K-V存储：Redis String类型）
    /// <summary>
    /// 获取单个最新数据缓存项
    /// 设计考量：反序列化失败时返回null，避免单个缓存项异常影响整体流程
    /// </summary>
    public async Task<ThingPropertyDataCacheItem?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }

        // Redis String Get操作
        var redisValue = await _redisDb.StringGetAsync(key);
        if (redisValue.IsNull)
        {
            return null;
        }

        // 反序列化（容错：反序列化失败返回null）
        try
        {
            return JsonSerializer.Deserialize<ThingPropertyDataCacheItem>(redisValue.ToString(), _jsonOptions);
        }
        catch (Exception)
        {
            // 记录日志（建议接入日志框架，如Serilog/Log4Net）
            await RemoveAsync(key, cancellationToken); // 清理损坏的缓存项
            return null;
        }
    }

    /// <summary>
    /// 批量获取最新数据缓存项
    /// 设计考量：使用Redis MGet批量操作，减少网络IO（N次Get → 1次MGet）
    /// </summary>
    public async Task<IDictionary<string, ThingPropertyDataCacheItem>> GetManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keys == null || !keys.Any())
        {
            throw new ArgumentException("缓存键列表不能为空", nameof(keys));
        }

        // 转换为RedisKey数组
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        // 批量获取
        var redisValues = await _redisDb.StringGetAsync(redisKeys);

        // 构建结果字典（键=原始缓存键，值=反序列化后的缓存项）
        var result = new Dictionary<string, ThingPropertyDataCacheItem>();
        for (int i = 0; i < redisKeys.Length; i++)
        {
            var key = redisKeys[i].ToString();
            var redisValue = redisValues[i];

            if (!redisValue.IsNull)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<ThingPropertyDataCacheItem>(redisValue.ToString(), _jsonOptions);
                    if (item != null)
                    {
                        result[key] = item;
                    }
                }
                catch (Exception)
                {
                    // 记录日志 + 清理损坏缓存项
                    await RemoveAsync(key, cancellationToken);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 设置单个最新数据缓存项
    /// 核心修复：解决TimeSpan?转Expiration的CS1503错误，先转换为非空TimeSpan
    /// </summary>
    public async Task SetAsync(
        string key,
        ThingPropertyDataCacheItem value,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 参数校验（即使单行也用{}，符合代码规范）
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (expiration == null)
        {
            throw new ArgumentNullException(nameof(expiration), "Redis缓存过期时间不能为空");
        }
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "Redis缓存过期时间必须大于0");
        }

        // 序列化缓存项
        string jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
        TimeSpan nonNullExpiration = expiration.Value;

        // 区分过期模式：Redis的StringSet支持绝对过期（TimeSpan/DateTime），滑动过期需自定义
        // 设计考量：Redis原生不支持滑动过期，此处通过"每次Get后重置过期时间"实现（上层Manager处理）
        await _redisDb.StringSetAsync(
            key,
            jsonValue,
            expiry: nonNullExpiration, // 绝对过期（TimeSpan转Expiration隐式转换）
            when: When.Always, // 覆盖已有值
            flags: CommandFlags.None);
    }

    #region 批量插入
    /// <summary>
    /// 批量设置最新数据缓存项
    /// 设计考量：使用Redis Pipeline批量执行，减少网络往返
    /// </summary>
    public async Task SetManyAsync(
        IDictionary<string, ThingPropertyDataCacheItem> keyValues,
        CacheExpireMode expireMode,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keyValues == null || !keyValues.Any())
        {
            throw new ArgumentException("缓存键值对不能为空", nameof(keyValues));
        }
        if (expiration == null)
        {
            throw new ArgumentNullException(nameof(expiration), "Redis缓存过期时间不能为空");
        }
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "Redis缓存过期时间必须大于0");
        }

        TimeSpan nonNullExpiration = expiration.Value;
        // 创建Redis Pipeline（批量执行命令）
        var batch = _redisDb.CreateBatch();
        foreach (var kvp in keyValues)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null)
            {
                continue; // 跳过无效项，不中断批量操作
            }

            string jsonValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
            batch.StringSetAsync(
                kvp.Key,
                jsonValue,
                expiry: nonNullExpiration,
                when: When.Always);
        }
        // 执行批量命令
        batch.Execute();
    }

    /// <summary>
    /// 批量设置最新数据缓存项
    /// 设计考量：使用Redis Pipeline批量执行，减少网络往返
    /// 修复点：
    /// 1. 移除无参batch.ExecuteAsync()调用（你的版本不支持）
    /// 2. 收集所有StringSetAsync的Task，通过Task.WhenAll执行批处理
    /// 3. 兼容IDatabase.ExecuteAsync仅有的两个重载（不影响批处理逻辑）
    /// </summary>
    //public async Task SetManyAsync(
    //    IDictionary<string, ThingPropertyDataCacheItem> keyValues,
    //    CacheExpireMode expireMode,
    //    TimeSpan? expiration,
    //    CancellationToken cancellationToken = default)
    //{
    //    cancellationToken.ThrowIfCancellationRequested();

    //    if (keyValues == null || !keyValues.Any())
    //    {
    //        throw new ArgumentException("缓存键值对不能为空", nameof(keyValues));
    //    }
    //    if (expiration == null)
    //    {
    //        throw new ArgumentNullException(nameof(expiration), "Redis缓存过期时间不能为空");
    //    }
    //    if (expiration <= TimeSpan.Zero)
    //    {
    //        throw new ArgumentOutOfRangeException(nameof(expiration), "Redis缓存过期时间必须大于0");
    //    }

    //    TimeSpan nonNullExpiration = expiration.Value;
    //    // 创建Redis Pipeline（批处理对象）
    //    var batch = _redisDb.CreateBatch();
    //    // 收集所有批处理命令的Task（核心：替代batch.ExecuteAsync()）
    //    var batchTasks = new List<Task>();

    //    foreach (var kvp in keyValues)
    //    {
    //        // 循环内增加取消检查（保证响应取消信号）
    //        cancellationToken.ThrowIfCancellationRequested();

    //        if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null)
    //        {
    //            continue; // 跳过无效项，不中断批量操作
    //        }

    //        string jsonValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
    //        // 将命令添加到批处理，并收集返回的Task
    //        Task setTask = batch.StringSetAsync(
    //            kvp.Key,
    //            jsonValue,
    //            expiry: nonNullExpiration,
    //            when: When.Always);

    //        batchTasks.Add(setTask);
    //    }

    //    // 核心修复：执行所有批处理命令（替代无参batch.ExecuteAsync()）
    //    // Task.WhenAll会触发批处理提交，并等待所有命令完成
    //    await Task.WhenAll(batchTasks).WaitAsync(cancellationToken);
    //}

    //public async Task SetManyAsync(
    //    IDictionary<string, ThingPropertyDataCacheItem> keyValues,
    //    CacheExpireMode expireMode,
    //    TimeSpan? expiration,
    //    CancellationToken cancellationToken = default)
    //{
    //    cancellationToken.ThrowIfCancellationRequested();

    //    if (keyValues == null || !keyValues.Any())
    //    {
    //        throw new ArgumentException("缓存键值对不能为空", nameof(keyValues));
    //    }
    //    if (expiration == null)
    //    {
    //        throw new ArgumentNullException(nameof(expiration), "Redis缓存过期时间不能为空");
    //    }
    //    if (expiration <= TimeSpan.Zero)
    //    {
    //        throw new ArgumentOutOfRangeException(nameof(expiration), "Redis缓存过期时间必须大于0");
    //    }

    //    TimeSpan nonNullExpiration = expiration.Value;
    //    // 转换为 StackExchange.Redis 专用的键值类型
    //    var redisKvPairs = new List<KeyValuePair<RedisKey, RedisValue>>();
    //    foreach (var kvp in keyValues)
    //    {
    //        // 循环内增加取消检查（保证响应取消信号）
    //        cancellationToken.ThrowIfCancellationRequested();

    //        if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null)
    //        {
    //            continue; // 跳过无效项，不中断批量操作
    //        }
    //        string jsonValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
    //        redisKvPairs.Add(new KeyValuePair<RedisKey, RedisValue>(kvp.Key, jsonValue));
    //    }

    //    // 区分过期模式：Redis的StringSet支持绝对过期（TimeSpan/DateTime），滑动过期需自定义
    //    // 设计考量：Redis原生不支持滑动过期，此处通过"每次Get后重置过期时间"实现（上层Manager处理）
    //    // 执行批量插入（优先使用StringSet批量重载，性能优于循环Batch）
    //    await _redisDb.StringSetAsync(
    //        redisKvPairs.ToArray(),
    //        expiry: nonNullExpiration, // 绝对过期（TimeSpan转Expiration隐式转换）
    //        when: When.Always,  // 覆盖已有值
    //        flags: CommandFlags.None);


    //    //// 分批执行批量插入（避免单次批量过大）
    //    //var batches = SplitList(redisKvPairs, _batchSize);

    //    //foreach (var batchItems in batches)
    //    //{
    //    //    cancellationToken.ThrowIfCancellationRequested();

    //    //    // 区分过期模式：Redis的StringSet支持绝对过期（TimeSpan/DateTime），滑动过期需自定义
    //    //    // 设计考量：Redis原生不支持滑动过期，此处通过"每次Get后重置过期时间"实现（上层Manager处理）
    //    //    // 执行批量插入（优先使用StringSet批量重载，性能优于循环Batch）
    //    //    bool batchSuccess = await _redisDb.StringSetAsync(
    //    //        batchItems.ToArray(),
    //    //        expiry: nonNullExpiration, // 绝对过期（TimeSpan转Expiration隐式转换）
    //    //        when: When.Always,  // 覆盖已有值
    //    //        flags: CommandFlags.None);
    //    //}
    //}
    #endregion

    /// <summary>
    /// 移除单个最新数据缓存项
    /// </summary>
    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }

        await _redisDb.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 批量移除最新数据缓存项
    /// </summary>
    public async Task RemoveManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keys == null || !keys.Any())
        {
            throw new ArgumentException("缓存键列表不能为空", nameof(keys));
        }

        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        await _redisDb.KeyDeleteAsync(redisKeys);
    }

    /// <summary>
    /// 检查最新数据缓存项是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }

        return await _redisDb.KeyExistsAsync(key);
    }

    /// <summary>
    /// 根据前缀获取所有匹配的缓存键
    /// 设计考量：Redis Keys命令性能较低，生产环境建议用Scan替代（避免阻塞Redis）
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Enumerable.Empty<string>();
        }

        // 生产环境建议替换为Scan（Keys命令在大数据量下阻塞Redis）
        var endpoint = _redisConnection.GetEndPoints().First();
        var server = _redisConnection.GetServer(endpoint);
        // 匹配前缀：prefix*（Redis通配符）
        var keys = server.Keys(
            database: _redisDb.Database,
            pattern: $"{prefix}*",
            pageSize: 1000)
            .Select(key => key.ToString())
            .ToList();

        return keys;
    }
    #endregion

    #region 历史数据操作（时序存储：Redis ZSet类型）
    /// <summary>
    /// 新增一条历史数据缓存项（ZSet Add）
    /// </summary>
    public async Task AddZSetItemAsync(
        string key,
        ThingPropertyDataCacheItem value,
        double score,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 参数校验
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // 序列化 + ZSet添加（Score=时间戳毫秒级）
        string jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
        await _redisDb.SortedSetAddAsync(key, jsonValue, score);
    }

    /// <summary>
    /// 批量新增历史数据缓存项（ZSet Batch Add）
    /// </summary>
    public async Task AddZSetItemsAsync(
        string key,
        IEnumerable<(ThingPropertyDataCacheItem Value, double Score)> valueScores,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }
        if (valueScores == null || !valueScores.Any())
        {
            throw new ArgumentException("缓存项列表不能为空", nameof(valueScores));
        }

        // 转换为Redis SortedSetEntry数组
        var entries = valueScores
            .Where(vs => vs.Value != null) // 过滤无效项
            .Select(vs => new SortedSetEntry(
                JsonSerializer.Serialize(vs.Value, _jsonOptions),
                vs.Score))
            .ToArray();

        // 批量添加到ZSet
        await _redisDb.SortedSetAddAsync(key, entries);
    }

    /// <summary>
    /// 按分数范围（时间戳）查询历史数据（ZSet RangeByScore）
    /// </summary>
    public async Task<(IList<ThingPropertyDataCacheItem> Items, int TotalCount)> GetZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 参数校验
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }
        if (pageIndex < 1)
        {
            pageIndex = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 100;
        }

        // 1. 获取总条数（ZSet Count）
        long totalCount = await _redisDb.SortedSetLengthAsync(key, minScore, maxScore);
        var items = new List<ThingPropertyDataCacheItem>();

        if (totalCount > 0)
        {
            // 2. 分页计算：Skip = (pageIndex-1)*pageSize，Take = pageSize
            long skip = (pageIndex - 1) * pageSize;
            // 3. 获取分页数据（ZSet RangeByScore）
            var redisValues = await _redisDb.SortedSetRangeByScoreAsync(
                key,
                start: minScore,
                stop: maxScore,
                skip: skip,
                take: pageSize);

            // 4. 反序列化
            foreach (var redisValue in redisValues)
            {
                if (!redisValue.IsNull)
                {
                    try
                    {
                        var item = JsonSerializer.Deserialize<ThingPropertyDataCacheItem>(redisValue.ToString(), _jsonOptions);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    catch (Exception)
                    {
                        // 记录日志 + 跳过损坏项
                    }
                }
            }
        }

        return (Items: items, TotalCount: (int)totalCount);
    }

    /// <summary>
    /// 删除指定分数范围的历史数据（ZSet RemoveRangeByScore）
    /// </summary>
    public async Task RemoveZSetByScoreRangeAsync(
        string key,
        double minScore,
        double maxScore,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空或空白", nameof(key));
        }

        await _redisDb.SortedSetRemoveRangeByScoreAsync(key, minScore, maxScore);
    }
    #endregion

    /// <summary>
    /// 清理过期缓存项（Redis依赖内置过期机制，此方法仅为适配接口）
    /// 设计考量：Redis自动清理过期Key，无需手动处理，此处空实现保证接口兼容性
    /// </summary>
    public Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Redis内置过期策略，无需手动清理
        return Task.CompletedTask;
    }

    #region 辅助方法 

    /// <summary>
    /// 拆分列表为指定大小的批次
    /// </summary>
    private List<List<T>> SplitList<T>(List<T> source, int batchSize)
    {
        var batches = new List<List<T>>();
        for (int i = 0; i < source.Count; i += batchSize)
        {
            batches.Add(source.Skip(i).Take(batchSize).ToList());
        }
        return batches;
    }

    #endregion

    #region 资源释放
    /// <summary>
    /// 释放Redis连接（适配IDisposable）
    /// </summary>
    public void Dispose()
    {
        // ConnectionMultiplexer是单例，此处不释放（由DI容器管理生命周期）
        GC.SuppressFinalize(this);
    }
    #endregion
}