using Artizan.IoT.Abstractions.Commons.Tracing;
using Artizan.IoT.Alinks.DataObjects;
using Artizan.IoT.Alinks.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp.Json;

namespace Artizan.IoT.Alinks.Parsers;

/// <summary>
/// AlinkJson协议解析器（核心业务组件，ABP社区版适配）
/// 【设计思路】
/// 1. 核心职责：原始AlinkJson消息 → 标准化DTO，仅做解析，无校验/处理逻辑（单一职责）；
/// 2. 设计原则：
///    - 策略模式：通过字段映射字典兼容设备侧的字段变体（如ProductKey、productKey）；
///    - 容错设计：捕获所有异常，返回结构化失败结果，避免链路崩溃；
///    - 性能优化：使用JsonElement而非dynamic，减少反序列化开销，适配高并发；
/// 3. 核心考量：
///    - 兼容性：适配不同设备的AlinkJson变体，降低设备接入成本；
///    - 可观测性：所有日志包含TraceId，便于问题溯源；
///    - 轻量：无商业包依赖，仅使用.NET原生JSON解析。
/// 【设计模式】：策略模式 + 单一职责模式 + 接口隔离模式
/// - 策略模式：字段映射字典封装字段匹配策略，可动态扩展；
/// - 接口隔离：IAlinkJsonParser接口隔离实现，便于单元测试Mock。
/// </summary>
public class AlinkJsonParser : IAlinkJsonParser
{
    private readonly ILogger<AlinkJsonParser> _logger;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ICurrentTraceIdAccessor _traceIdAccessor;

    /// <summary>
    /// AlinkJson字段映射策略（核心：兼容不同设备的字段变体）
    /// 【设计考量】：可动态扩展映射规则，无需修改核心解析逻辑（开闭原则）。
    /// </summary>
    private readonly Dictionary<string, string> _alinkFieldMappings = new()
{
    { "productKey", "productKey" },
    { "deviceName", "deviceName" },
    { "method", "method" },
    { "params", "params" },
    { "id", "traceId" },
    { "timestamp", "timestamp" },
    // 兼容大小写变体
    { "ProductKey", "productKey" },
    { "DeviceName", "deviceName" },
    { "Method", "method" }
};

    /// <summary>
    /// 构造函数（依赖注入，ABP社区版DI自动注入）
    /// 【设计考量】：依赖接口而非实现，便于替换日志/JSON序列化器（依赖倒置）。
    /// </summary>
    public AlinkJsonParser(
        ILogger<AlinkJsonParser> logger,
        IJsonSerializer jsonSerializer,
        ICurrentTraceIdAccessor traceIdAccessor)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _traceIdAccessor = traceIdAccessor;
    }

    /// <summary>
    /// 核心解析方法
    /// 【核心流程】：TraceId生成 → 空值校验 → JSON解析 → 字段提取 → ModuleCode提取 → 结果封装
    /// </summary>
    public AlinkHandleResult Parse(string rawMessage, string rawTopic)
    {
        // 预生成TraceId（即使解析失败，也有唯一标识用于日志）
        string traceId = Guid.NewGuid().ToString("N");

        try
        {
            _logger.LogDebug("[TraceId:{TraceId}] 开始解析AlinkJson消息 | 原始内容={RawMessage}", traceId, rawMessage);

            // 空值快速失败
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                _logger.LogWarning("[TraceId:{TraceId}] AlinkJson解析失败 | 原因=消息为空", traceId);
                return AlinkHandleResult.ParseFailed(traceId, "消息内容为空", rawMessage);
            }

            // 高性能JSON解析（JsonElement避免反序列化到动态对象）
            var alinkObj = JsonSerializer.Deserialize<JsonElement>(rawMessage);
            var dataContext = new AlinkDataContext(_traceIdAccessor)
            {
                //RawMessage = rawMessage,
                //RawTopic = rawTopic
            };

            // 提取设备侧传递的TraceId（优先复用）
            if (TryGetJsonProperty(alinkObj, "id", out var idValue))
            {
                traceId = idValue;
                dataContext.TraceId = traceId;
                // 同步到上下文，后续流程复用
                using (_traceIdAccessor.Change(traceId))
                {
                    _logger.LogDebug("[TraceId:{TraceId}] 同步设备侧TraceId到上下文", traceId);
                }
            }

            // 必选字段提取（缺失则直接失败）
            if (!TryGetJsonProperty(alinkObj, "productKey", out var productKey))
            {
                _logger.LogWarning("[TraceId:{TraceId}] AlinkJson解析失败 | 原因=缺失productKey字段", traceId);
                return AlinkHandleResult.ParseFailed(traceId, "缺失productKey字段", rawMessage);
            }
            //TODO:dataContext.ProductKey = productKey;

            if (!TryGetJsonProperty(alinkObj, "deviceName", out var deviceName))
            {
                _logger.LogWarning("[TraceId:{TraceId}] AlinkJson解析失败 | 原因=缺失deviceName字段", traceId);
                return AlinkHandleResult.ParseFailed(traceId, "缺失deviceName字段", rawMessage);
            }
            //TODO:dataContext.DeviceName = deviceName;

            if (!TryGetJsonProperty(alinkObj, "method", out var method))
            {
                _logger.LogWarning("[TraceId:{TraceId}] AlinkJson解析失败 | 原因=缺失method字段", traceId);
                return AlinkHandleResult.ParseFailed(traceId, "缺失method字段", rawMessage);
            }
            //TODO:dataContext.Method = method;

            // 可选字段提取（缺失则赋默认值）
            if (TryGetJsonProperty(alinkObj, "params", out var paramStr))
            {
                // 统一转换为JSON字符串，兼容Params是原始值/对象的情况
                //TODO:dataContext.Params = paramStr.StartsWith("{") ? paramStr : JsonSerializer.Serialize(paramStr);
            }
            if (TryGetJsonProperty(alinkObj, "timestamp", out var timestampStr) && long.TryParse(timestampStr, out var timestamp))
            {
                //TODO:dataContext.Timestamp = timestamp;
            }

            //TODO: _logger.LogInformation("[TraceId:{TraceId}] AlinkJson解析成功 | ProductKey={PK} | DeviceName={DN} | Method={Method}",
            //TODO:traceId, dataContext.ProductKey, dataContext.DeviceName, dataContext.Method);

            return AlinkHandleResult.Success(dataContext);
        }
        catch (JsonException ex)
        {
            // JSON格式异常捕获（如非标准JSON）
            _logger.LogError(ex, "[TraceId:{TraceId}] AlinkJson解析失败 | 原因=JSON格式非法 | 原始消息={RawMessage}",
                traceId, rawMessage);
            return AlinkHandleResult.ParseFailed(traceId, $"JSON格式非法：{ex.Message}", rawMessage);
        }
        catch (Exception ex)
        {
            // 兜底异常捕获（防止未处理异常导致进程崩溃）
            _logger.LogError(ex, "[TraceId:{TraceId}] AlinkJson解析失败 | 原因=未知异常 | 原始消息={RawMessage}",
                traceId, rawMessage);
            return AlinkHandleResult.ParseFailed(traceId, $"未知异常：{ex.Message}", rawMessage);
        }
    }


    /// <summary>
    /// 安全获取JSON属性值（策略模式核心方法）
    /// 【设计思路】：封装字段提取逻辑，避免重复的try-catch和空值判断，提升可维护性。
    /// </summary>
    private bool TryGetJsonProperty(JsonElement jsonElement, string fieldName, out string value)
    {
        value = string.Empty;

        // 第一步：匹配原始字段名
        if (jsonElement.TryGetProperty(fieldName, out var property))
        {
            value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString() ?? string.Empty,
                JsonValueKind.Number => property.GetRawText(), // 数字保留原始文本，避免精度丢失
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => string.Empty
            };
            return !string.IsNullOrWhiteSpace(value);
        }

        // 第二步：匹配映射字段名（兼容变体）
        if (_alinkFieldMappings.TryGetValue(fieldName, out var mappedField)
            && jsonElement.TryGetProperty(mappedField, out var mappedProperty))
        {
            value = mappedProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }
}
