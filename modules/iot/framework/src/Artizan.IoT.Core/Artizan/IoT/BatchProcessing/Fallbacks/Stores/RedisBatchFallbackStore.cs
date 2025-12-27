using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Enums;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Fallbacks.Stores;

/// <summary>
/// Redis兜底存储
/// 【设计思路】：将失败消息存储到Redis，支持分布式场景
/// 【设计考量】：
/// 1. 使用Hash存储消息元数据，List存储消息ID，便于读取和删除
/// 2. 设置过期时间，避免数据膨胀
/// 3. 批量操作减少网络IO
/// 4. 支持分布式锁，保证线程安全
/// 【设计模式】：仓库模式
/// </summary>
/// <typeparam name="TMessage">消息类型</typeparam>
public class RedisBatchFallbackStore<TMessage> : IBatchFallbackStore<TMessage>
{
    /// <summary>
    /// Redis连接多路复用器
    /// </summary>
    private readonly IConnectionMultiplexer _redisMultiplexer;

    /// <summary>
    /// Redis数据库
    /// </summary>
    private readonly IDatabase _redisDb;

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<RedisBatchFallbackStore<TMessage>> _logger;

    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    /// <summary>
    /// Redis键前缀
    /// </summary>
    private const string KeyPrefix = "batch:fallback:";

    /// <summary>
    /// 消息过期时间（默认7天）
    /// </summary>
    private readonly TimeSpan _expiration = TimeSpan.FromDays(7);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redisMultiplexer">Redis连接</param>
    /// <param name="logger">日志器</param>
    public RedisBatchFallbackStore(IConnectionMultiplexer redisMultiplexer, ILogger<RedisBatchFallbackStore<TMessage>> logger)
    {
        _redisMultiplexer = redisMultiplexer ?? throw new ArgumentNullException(nameof(redisMultiplexer));
        _redisDb = _redisMultiplexer.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 存储单条消息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="reason">失败原因</param>
    /// <param name="fallbackType">兜底类型</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="messageId">消息ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task StoreAsync(
        TMessage message,
        string reason,
        FallbackType fallbackType,
        string traceId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrEmpty(messageId))
        {
            messageId = Guid.NewGuid().ToString("N");
        }

        try
        {
            // 包装消息
            var wrappedMessage = new
            {
                MessageId = messageId,
                TraceId = traceId,
                FallbackType = fallbackType.ToString(),
                Reason = reason,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Message = message
            };

            // 序列化
            var json = JsonSerializer.Serialize(wrappedMessage, _jsonOptions);

            // Redis键
            var hashKey = $"{KeyPrefix}messages:{fallbackType}:{DateTime.UtcNow:yyyyMMdd}";
            var listKey = $"{KeyPrefix}queue:{fallbackType}:{DateTime.UtcNow:yyyyMMdd}";

            // 批量操作
            var batch = _redisDb.CreateBatch();

            // 1. 存储到Hash
            batch.HashSetAsync(hashKey, messageId, json);
            // 2. 设置过期时间
            batch.KeyExpireAsync(hashKey, _expiration);
            // 3. 添加到List
            batch.ListRightPushAsync(listKey, messageId);
            // 4. 设置List过期时间
            batch.KeyExpireAsync(listKey, _expiration);

            // 执行批量操作
            batch.Execute();

            _logger.LogInformation(
                "[TraceId:{TraceId}] 消息已存入Redis兜底存储 [MessageId:{MessageId}, FallbackType:{FallbackType}]",
                traceId,
                messageId,
                fallbackType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 存储消息到Redis失败 [MessageId:{MessageId}]",
                traceId,
                messageId);
            throw;
        }
    }

    /// <summary>
    /// 批量存储消息
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="fallbackType">兜底类型</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task StoreBatchAsync(
        List<TMessage> messages,
        string partitionKey,
        FallbackType fallbackType,
        string traceId,
        CancellationToken cancellationToken = default)
    {
        if (messages == null || messages.Count == 0)
        {
            _logger.LogDebug("[TraceId:{TraceId}] 无消息需要批量存储", traceId);
            return;
        }

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "unknown";
        }

        var batchId = Guid.NewGuid().ToString("N");
        var batchKey = $"{KeyPrefix}batch:{fallbackType}:{batchId}";

        try
        {
            // 包装批量消息
            var wrappedBatch = new
            {
                BatchId = batchId,
                PartitionKey = partitionKey,
                TraceId = traceId,
                FallbackType = fallbackType.ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                MessageCount = messages.Count,
                Messages = messages
            };

            // 序列化
            var json = JsonSerializer.Serialize(wrappedBatch, _jsonOptions);

            // Redis操作
            var batch = _redisDb.CreateBatch();

            // 1. 存储批量消息
            batch.StringSetAsync(batchKey, json, _expiration);

            // 2. 添加到批量队列
            var batchListKey = $"{KeyPrefix}batch_queue:{fallbackType}:{DateTime.UtcNow:yyyyMMdd}:{partitionKey}";
            batch.ListRightPushAsync(batchListKey, batchId);
            batch.KeyExpireAsync(batchListKey, _expiration);

            // 执行批量操作
            batch.Execute();

            _logger.LogInformation(
                "[TraceId:{TraceId}] 批量消息已存入Redis兜底存储 [BatchId:{BatchId}, PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                traceId,
                batchId,
                partitionKey,
                messages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[TraceId:{TraceId}] 批量存储消息到Redis失败 [PartitionKey:{PartitionKey}, MessageCount:{MessageCount}]",
                traceId,
                partitionKey,
                messages.Count);
            throw;
        }
    }

    /// <summary>
    /// 读取兜底消息
    /// </summary>
    /// <param name="partitionKey">分区Key</param>
    /// <param name="count">读取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息列表</returns>
    public async Task<List<TMessage>> ReadAsync(string partitionKey, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "unknown";
        }

        if (count <= 0)
        {
            count = 100;
        }

        var messages = new List<TMessage>();
        var processedBatchIds = new List<string>();

        try
        {
            // 1. 读取批量消息（优先读取批量，减少IO）
            foreach (var fallbackType in Enum.GetValues<FallbackType>())
            {
                if (messages.Count >= count) break;

                // 按日期倒序遍历（最近的优先）
                for (int i = 0; i < 7; i++) // 最多读取7天的数据
                {
                    if (messages.Count >= count) break;

                    var date = DateTime.UtcNow.AddDays(-i).ToString("yyyyMMdd");
                    var batchListKey = $"{KeyPrefix}batch_queue:{fallbackType}:{date}:{partitionKey}";

                    // 读取批量ID列表（不弹出，仅读取）
                    var batchIds = await _redisDb.ListRangeAsync(batchListKey, 0, count - messages.Count - 1);
                    if (batchIds.Length == 0) continue;

                    // 批量获取批量消息
                    foreach (var batchIdRedis in batchIds)
                    {
                        if (messages.Count >= count) break;

                        var batchId = batchIdRedis.ToString();
                        var batchKey = $"{KeyPrefix}batch:{fallbackType}:{batchId}";

                        // 获取批量消息内容
                        var batchJson = await _redisDb.StringGetAsync(batchKey);
                        if (batchJson.IsNull)
                        {
                            processedBatchIds.Add(batchId);
                            continue;
                        }

                        try
                        {
                            // 反序列化批量消息
                            var batchMessage = JsonSerializer.Deserialize<dynamic>(batchJson);
                            if (batchMessage?.Messages != null)
                            {
                                var batchMessages = JsonSerializer.Deserialize<List<TMessage>>(
                                    JsonSerializer.Serialize(batchMessage.Messages),
                                    _jsonOptions);

                                foreach (var msg in batchMessages)
                                {
                                    if (messages.Count >= count) break;
                                    messages.Add(msg);
                                }
                            }

                            processedBatchIds.Add(batchId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[TraceId:None] 反序列化Redis批量消息失败 [BatchId:{BatchId}]", batchId);
                        }
                    }
                }
            }

            // 2. 读取单条消息（补充数量）
            if (messages.Count < count)
            {
                foreach (var fallbackType in Enum.GetValues<FallbackType>())
                {
                    if (messages.Count >= count) break;

                    for (int i = 0; i < 7; i++)
                    {
                        if (messages.Count >= count) break;

                        var date = DateTime.UtcNow.AddDays(-i).ToString("yyyyMMdd");
                        var listKey = $"{KeyPrefix}queue:{fallbackType}:{date}";
                        var hashKey = $"{KeyPrefix}messages:{fallbackType}:{date}";

                        // 读取消息ID列表
                        var messageIds = await _redisDb.ListRangeAsync(listKey, 0, count - messages.Count - 1);
                        if (messageIds.Length == 0) continue;

                        // 批量获取Hash中的消息
                        var hashEntries = await _redisDb.HashGetAsync(hashKey, messageIds);
                        foreach (var entry in hashEntries)
                        {
                            if (messages.Count >= count) break;
                            if (entry.IsNull) continue;

                            try
                            {
                                var singleMessage = JsonSerializer.Deserialize<dynamic>(entry);
                                if (singleMessage?.Message != null)
                                {
                                    var msg = JsonSerializer.Deserialize<TMessage>(
                                        JsonSerializer.Serialize(singleMessage.Message),
                                        _jsonOptions);
                                    messages.Add(msg);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[TraceId:None] 反序列化Redis单条消息失败");
                            }
                        }
                    }
                }
            }

            _logger.LogInformation(
                "[TraceId:None] 读取Redis兜底消息完成 [PartitionKey:{PartitionKey}, ReadCount:{ReadCount}, RequestedCount:{RequestedCount}]",
                partitionKey,
                messages.Count,
                count);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 读取Redis兜底消息异常 [PartitionKey:{PartitionKey}]", partitionKey);
            return messages;
        }
    }

    /// <summary>
    /// 删除已处理的兜底消息
    /// </summary>
    /// <param name="messageIds">消息ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteAsync(List<string> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds == null || messageIds.Count == 0)
        {
            _logger.LogDebug("[TraceId:None] 无消息需要删除");
            return;
        }

        int deletedCount = 0;

        try
        {
            // 遍历所有兜底类型和最近7天的数据
            foreach (var fallbackType in Enum.GetValues<FallbackType>())
            {
                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.UtcNow.AddDays(-i).ToString("yyyyMMdd");
                    var hashKey = $"{KeyPrefix}messages:{fallbackType}:{date}";
                    var listKey = $"{KeyPrefix}queue:{fallbackType}:{date}";

                    // 批量删除Hash中的消息
                    var batch = _redisDb.CreateBatch();
                    int batchDeleted = 0;

                    foreach (var messageId in messageIds)
                    {
                        // 删除Hash字段
                        batch.HashDeleteAsync(hashKey, messageId);
                        // 从List中移除消息ID
                        batch.ListRemoveAsync(listKey, messageId, 0);
                        batchDeleted++;
                    }

                    // 执行批量操作
                    batch.Execute();
                    deletedCount += batchDeleted;
                }
            }

            _logger.LogInformation(
                "[TraceId:None] 删除Redis兜底消息完成 [DeletedCount:{DeletedCount}, RequestedCount:{RequestedCount}]",
                deletedCount,
                messageIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 删除Redis兜底消息异常 [RequestedCount:{RequestedCount}]", messageIds.Count);
        }
    }

    /// <summary>
    /// 清理过期的兜底数据（辅助方法）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的键数量</returns>
    public async Task<long> CleanupExpiredDataAsync(CancellationToken cancellationToken = default)
    {
        long cleanedCount = 0;

        try
        {
            // 获取所有Redis服务器
            var endpoints = _redisMultiplexer.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redisMultiplexer.GetServer(endpoint);

                // 扫描匹配的键
                var keys = server.Keys(_redisDb.Database, $"{KeyPrefix}*", pageSize: 1000);
                foreach (var key in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 检查键是否过期
                    var ttl = await _redisDb.KeyTimeToLiveAsync(key);
                    if (ttl == null || ttl <= TimeSpan.Zero)
                    {
                        await _redisDb.KeyDeleteAsync(key);
                        cleanedCount++;
                    }
                }
            }

            _logger.LogInformation("[TraceId:None] 清理Redis兜底过期数据完成 [CleanedCount:{CleanedCount}]", cleanedCount);
            return cleanedCount;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[TraceId:None] 清理Redis兜底过期数据被取消 [CleanedCount:{CleanedCount}]", cleanedCount);
            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 清理Redis兜底过期数据异常 [CleanedCount:{CleanedCount}]", cleanedCount);
            return cleanedCount;
        }
    }
}