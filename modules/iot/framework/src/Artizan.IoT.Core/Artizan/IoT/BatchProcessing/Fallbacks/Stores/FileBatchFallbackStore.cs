using Artizan.IoT.BatchProcessing.Abstractions;
using Artizan.IoT.BatchProcessing.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.BatchProcessing.Fallbacks.Stores;

/// <summary>
/// 文件兜底存储
/// 【设计思路】：将失败消息存储到本地文件，保证数据不丢失
/// 【设计考量】：
/// 1. 按日期+分区创建目录，便于管理和查找
/// 2. 使用JSON序列化，便于阅读和解析
/// 3. 批量存储时合并文件，减少IO操作
/// 4. 线程安全的文件写入，避免文件冲突
/// 【设计模式】：仓库模式
/// </summary>
/// <typeparam name="TMessage">消息类型</typeparam>
public class FileBatchFallbackStore<TMessage> : IBatchFallbackStore<TMessage>
{
    /// <summary>
    /// 存储根路径
    /// </summary>
    private readonly string _rootPath;

    /// <summary>
    /// 日志器
    /// </summary>
    private readonly ILogger<FileBatchFallbackStore<TMessage>> _logger;

    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 文件写入锁
    /// </summary>
    private readonly object _writeLock = new object();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rootPath">存储根路径</param>
    /// <param name="logger">日志器</param>
    public FileBatchFallbackStore(string rootPath, ILogger<FileBatchFallbackStore<TMessage>> logger)
    {
        _rootPath = string.IsNullOrEmpty(rootPath) ? "./FallbackStore" : rootPath;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 创建目录（如果不存在）
        Directory.CreateDirectory(_rootPath);
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

        // 包装消息（添加元数据）
        var wrappedMessage = new
        {
            MessageId = messageId,
            TraceId = traceId,
            FallbackType = fallbackType.ToString(),
            Reason = reason,
            Timestamp = DateTime.UtcNow.ToString("o"),
            Message = message
        };

        // 序列化消息
        var json = JsonSerializer.Serialize(wrappedMessage, _jsonOptions);

        // 获取文件路径
        var filePath = GetFilePath(fallbackType, messageId);

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 写入文件（线程安全）
        lock (_writeLock)
        {
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        _logger.LogInformation(
            "[TraceId:{TraceId}] 消息已存入文件兜底存储 [MessageId:{MessageId}, FilePath:{FilePath}]",
            traceId,
            messageId,
            filePath);

        await Task.CompletedTask;
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
            await Task.CompletedTask;
            return;
        }

        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "unknown";
        }

        // 包装批量消息
        var wrappedBatch = new
        {
            BatchId = Guid.NewGuid().ToString("N"),
            PartitionKey = partitionKey,
            TraceId = traceId,
            FallbackType = fallbackType.ToString(),
            Timestamp = DateTime.UtcNow.ToString("o"),
            MessageCount = messages.Count,
            Messages = messages
        };

        // 序列化
        var json = JsonSerializer.Serialize(wrappedBatch, _jsonOptions);

        // 获取批量文件路径
        var batchFileName = $"batch_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.json";
        var filePath = Path.Combine(
            _rootPath,
            fallbackType.ToString(),
            DateTime.UtcNow.ToString("yyyyMMdd"),
            partitionKey,
            batchFileName);

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 写入文件
        lock (_writeLock)
        {
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        _logger.LogInformation(
            "[TraceId:{TraceId}] 批量消息已存入文件兜底存储 [BatchId:{BatchId}, MessageCount:{MessageCount}, FilePath:{FilePath}]",
            traceId,
            wrappedBatch.BatchId,
            messages.Count,
            filePath);

        await Task.CompletedTask;
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
        var processedFiles = new List<string>();

        try
        {
            // 遍历所有兜底类型目录
            foreach (var fallbackTypeDir in Directory.GetDirectories(_rootPath))
            {
                // 遍历日期目录
                foreach (var dateDir in Directory.GetDirectories(fallbackTypeDir))
                {
                    // 遍历分区目录
                    var partitionDir = Path.Combine(dateDir, partitionKey);
                    if (!Directory.Exists(partitionDir))
                    {
                        continue;
                    }

                    // 读取文件
                    foreach (var filePath in Directory.GetFiles(partitionDir, "*.json"))
                    {
                        if (messages.Count >= count)
                        {
                            break;
                        }

                        try
                        {
                            var json = await File.ReadAllTextAsync(filePath, cancellationToken);

                            // 尝试解析单条消息
                            try
                            {
                                var singleMessage = JsonSerializer.Deserialize<dynamic>(json);
                                if (singleMessage?.Message != null)
                                {
                                    var message = JsonSerializer.Deserialize<TMessage>(
                                        JsonSerializer.Serialize(singleMessage.Message),
                                        _jsonOptions);
                                    messages.Add(message);
                                }
                            }
                            catch
                            {
                                // 尝试解析批量消息
                                var batchMessage = JsonSerializer.Deserialize<dynamic>(json);
                                if (batchMessage?.Messages != null)
                                {
                                    var batchMessages = JsonSerializer.Deserialize<List<TMessage>>(
                                        JsonSerializer.Serialize(batchMessage.Messages),
                                        _jsonOptions);

                                    foreach (var msg in batchMessages)
                                    {
                                        if (messages.Count >= count)
                                        {
                                            break;
                                        }
                                        messages.Add(msg);
                                    }
                                }
                            }

                            processedFiles.Add(filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[TraceId:None] 读取兜底文件失败 [FilePath:{FilePath}]", filePath);
                        }
                    }
                }
            }

            _logger.LogInformation(
                "[TraceId:None] 读取兜底消息完成 [PartitionKey:{PartitionKey}, ReadCount:{ReadCount}, RequestedCount:{RequestedCount}]",
                partitionKey,
                messages.Count,
                count);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TraceId:None] 读取兜底消息异常 [PartitionKey:{PartitionKey}]", partitionKey);
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
            await Task.CompletedTask;
            return;
        }

        int deletedCount = 0;

        // 遍历所有目录查找并删除对应文件
        foreach (var fallbackTypeDir in Directory.GetDirectories(_rootPath))
        {
            foreach (var dateDir in Directory.GetDirectories(fallbackTypeDir))
            {
                foreach (var partitionDir in Directory.GetDirectories(dateDir))
                {
                    foreach (var filePath in Directory.GetFiles(partitionDir, "*.json"))
                    {
                        try
                        {
                            var fileName = Path.GetFileNameWithoutExtension(filePath);

                            // 单条消息文件：包含MessageId
                            if (fileName.StartsWith("msg_") && messageIds.Any(id => fileName.Contains(id)))
                            {
                                File.Delete(filePath);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[TraceId:None] 删除兜底文件失败 [FilePath:{FilePath}]", filePath);
                        }
                    }
                }
            }
        }

        _logger.LogInformation(
            "[TraceId:None] 删除兜底消息完成 [DeletedCount:{DeletedCount}, RequestedCount:{RequestedCount}]",
            deletedCount,
            messageIds.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取单条消息的文件路径
    /// </summary>
    /// <param name="fallbackType">兜底类型</param>
    /// <param name="messageId">消息ID</param>
    /// <returns>文件路径</returns>
    private string GetFilePath(FallbackType fallbackType, string messageId)
    {
        var fileName = $"msg_{messageId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.json";
        return Path.Combine(
            _rootPath,
            fallbackType.ToString(),
            DateTime.UtcNow.ToString("yyyyMMdd"),
            "single",
            fileName);
    }
}
