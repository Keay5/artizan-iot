using Artizan.IoT.Mqtts.Messages.Parsers;
using Artizan.IoTHub.Mqtts.Messages;
using MQTTnet.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Artizan.IoT.Mqtts.Messages;

/// <summary>
/// MQTT消息处理上下文（全流程唯一数据载体，你的核心类）
/// 路由系统适配说明：
/// 1. 路由系统初始化时调用构造函数创建实例；
/// 2. 路由匹配后自动填充TopicPlaceholderValues、ProductKey、DeviceName；
/// 3. 路由调度Handler时，将此上下文传入，Handler自主更新解析结果/步骤结果；
/// 4. 路由结束后，通过IsOverallSuccess判断整体状态，ToLogDictionary输出日志。
/// 设计理念：
/// - 中心化：聚合消息全生命周期数据，替代多参数传递，降低耦合；
/// - 不可变性：核心标识（TraceId/ClientId/Topic）初始化后不可改，避免数据篡改；
/// - 线程安全：执行步骤用ConcurrentDictionary，支持多线程并行处理；
/// - 可追踪：内置TraceId+时间戳+耗时统计，全链路问题可排查；
/// - 扩展性：Extension字段支持业务自定义数据，无需修改类结构。
/// 
/// </summary>
public class MqttMessageContext : IDisposable
{
    #region 基础层（路由系统初始化+填充，核心标识不可变）
    /// <summary>
    /// MQTT客户端ID（路由系统从MQTT连接中提取，关联唯一设备）
    /// 设计：不可变，确保全流程客户端身份唯一
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 全链路追踪ID（路由系统自动生成或复用上游分布式追踪ID）
    /// 设计：全局唯一，日志/监控/链路追踪的核心关联字段
    /// </summary>
    public string TraceId { get; protected set; }

    /// <summary>
    /// 消息接收时间（UTC时间，路由系统初始化时自动赋值）
    /// 设计：统一时间基准，避免多服务器时区差异导致的时间混乱
    /// </summary>
    public DateTime ReceiveTimeUtc { get; } = DateTime.UtcNow;

    /// <summary>
    /// 原始MQTT消息（路由系统接收消息后封装，仅引用不拷贝，降低内存占用）
    /// 设计：保留原始消息元数据，避免二次拷贝大Payload
    /// </summary>
    public MqttRawMessage RawMessage { get; }

    /// <summary>
    /// MQTT Topic（从RawMessage提取，路由系统匹配的核心字段）
    /// 设计：冗余存储，避免频繁从RawMessage中读取，提升路由匹配效率
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// 路由系统核心字段：从Topic解析的占位符值（如productKey/deviceName/tsl.event.identifier）
    /// 设计：路由系统自动填充，Handler直接使用，无需重复解析Topic
    /// 示例：Topic="/sys/pk123/dn456/event" → 存储{"productKey":"pk123", "deviceName":"dn456"}
    /// </summary>
    public Dictionary<string, string> TopicPlaceholderValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 产品唯一标识（路由系统从占位符中提取，核心业务字段）
    /// 设计：独立存储，简化Handler业务逻辑访问，避免重复从占位符字典取值
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备唯一标识（路由系统从占位符中提取，核心业务字段）
    /// 设计：同ProductKey，提升业务代码可读性和访问效率
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    #endregion

    #region 解析层（Handler自主更新，路由系统仅读取）
    /// <summary>
    /// 消息解析类型（Handler更新，标记ParsedData的数据格式）
    /// 设计：与ParsedData配合，避免类型转换错误
    /// </summary>
    public MqttMessageDataParseType ParseType { get; private set; }

    /// <summary>
    /// 解析后的结构化数据（Handler更新，路由系统不干预解析逻辑）
    /// 设计：object类型避免泛型导致的类膨胀，通过ParseType约束实际类型
    /// </summary>
    public object? ParsedData { get; private set; }

    /// <summary>
    /// 解析是否成功（Handler更新，路由系统判断是否继续执行后续步骤）
    /// 设计：简化路由系统逻辑，无需通过异常间接判断解析状态
    /// </summary>
    public bool IsParsedSuccess { get; private set; }

    /// <summary>
    /// 解析耗时（Handler更新，路由系统用于性能监控）
    /// 设计：量化解析性能，便于排查解析瓶颈
    /// </summary>
    public TimeSpan ParsedElapsed { get; private set; }

    /// <summary>
    /// 解析错误信息（Handler更新，路由系统用于日志记录）
    /// 设计：集中存储解析失败原因，便于问题排查
    /// </summary>
    public string? ParseErrorMsg { get; private set; }
    #endregion

    #region 执行层（路由系统+Handler协作更新，线程安全）
    /// <summary>
    /// 线程安全的步骤执行结果容器（路由系统记录路由步骤，Handler记录业务步骤）
    /// 设计：ConcurrentDictionary保障多线程并行更新安全，支持并行处理流程
    /// </summary>
    private readonly ConcurrentDictionary<string, MqttMessageProcessStepExecuteResult> _stepResults = new();

    /// <summary>
    /// 整体执行是否成功（路由系统判断：解析成功+所有步骤成功）
    /// 设计：提供最终状态，简化上游系统结果判断
    /// </summary>
    public bool IsOverallSuccess => IsParsedSuccess && _stepResults.Values.All(s => s.IsSuccess);

    /// <summary>
    /// 全局致命异常（路由系统或Handler记录，中断整个流程的错误）
    /// 设计：区分步骤异常和流程异常，便于定位致命问题
    /// </summary>
    public Exception? GlobalException { get; private set; }
    #endregion

    #region 扩展层（业务自定义，路由系统不干预）
    /// <summary>
    /// 扩展字段容器（Handler或业务系统动态添加临时数据）
    /// 设计：遵循开放封闭原则，避免频繁修改上下文类结构
    /// 示例：存储权限信息、中间计算结果、自定义标签等
    /// </summary>
    public MqttMessageContextExtension Extension { get; } = new();
    #endregion

    /// <summary>
    /// 异步操作取消令牌（支持消息处理中途取消，如客户端断开连接、超时终止）
    /// 设计：默认CancellationToken.None，外部可通过构造函数传入，适配异步流程管控
    /// </summary>
    public CancellationToken CancellationToken { get; }

    #region 构造函数（路由系统初始化时调用，严格参数校验）
    /// <summary>
    /// 路由系统接收消息初期调用（未解析ProductKey/DeviceName）
    /// 场景：消息刚接收，路由匹配前，仅需基础Topic和Payload
    /// </summary>
    public MqttMessageContext(
        MqttRawMessage mqttRawMessage,
        string mqttClientId,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        // 公共字段赋值
        TraceId = traceId ?? Guid.NewGuid().ToString("N");   // 路由系统自动生成或复用TraceId
        CancellationToken = cancellationToken; // 复用取消令牌赋值

        // 严格参数校验，避免空值流入下游（生产环境fail-fast原则）
        ClientId = mqttClientId ?? throw new ArgumentNullException(nameof(mqttClientId), "客户端ID不能为空");
        Topic = mqttRawMessage.Topic ?? throw new ArgumentNullException(nameof(mqttRawMessage.Topic), "Topic不能为空");
        RawMessage = mqttRawMessage;
    }

    /// <summary>
    /// 路由系统匹配后调用（已解析ProductKey/DeviceName）
    /// 场景：路由匹配成功，准备调用Handler处理业务
    /// </summary>
    [Obsolete("推荐使用基础构造函数，路由系统通过TopicPlaceholderValues赋值ProductKey/DeviceName")]
    public MqttMessageContext(
        MqttRawMessage mqttRawMessage,
        string mqttClientId,
        string productKey,
        string deviceName,
        string? traceId = null,
        CancellationToken cancellationToken = default)
        : this(mqttRawMessage, mqttClientId, traceId, cancellationToken) // 调用基础构造函数复用逻辑
    {
        // 特有逻辑：ProductKey/DeviceName的校验和赋值
        ProductKey = productKey ?? throw new ArgumentNullException(nameof(productKey), "产品标识不能为空");
        DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName), "设备标识不能为空");
    }
    #endregion

    #region 解析结果更新（Handler调用，路由系统仅读取）
    /// <summary>
    /// 标记解析成功（Handler解析消息后调用）
    /// 设计：仅允许调用一次，避免重复设置解析结果
    /// </summary>
    public void SetParseSuccess(MqttMessageDataParseType parseType, object parsedData, TimeSpan elapsed)
    {
        if (IsParsedSuccess) throw new InvalidOperationException($"[{TraceId}] 解析结果已设置，不允许重复调用");
        ParseType = parseType;
        ParsedData = parsedData ?? throw new ArgumentNullException(nameof(parsedData), $"[{TraceId}] 解析数据不能为null");
        IsParsedSuccess = true;
        ParsedElapsed = elapsed;
        ParseErrorMsg = null;
    }

    /// <summary>
    /// 标记解析失败（Handler解析消息失败后调用）
    /// 设计：记录错误信息和异常，路由系统据此判断是否终止流程
    /// </summary>
    public void SetParseFailed(string errorMsg, TimeSpan elapsed, Exception? exception = null)
    {
        if (IsParsedSuccess) throw new InvalidOperationException($"[{TraceId}] 解析已成功，无法标记为失败");
        ParseType = MqttMessageDataParseType.Unknown;
        ParsedData = null;
        IsParsedSuccess = false;
        ParsedElapsed = elapsed;
        ParseErrorMsg = string.IsNullOrWhiteSpace(errorMsg)
            ? $"[{TraceId}] 解析失败（无详细信息）"
            : $"[{TraceId}] {errorMsg}" + (exception != null ? $" | 异常: {exception.Message}" : string.Empty);
        GlobalException = exception; // 解析失败视为全局异常，路由系统终止流程
    }
    #endregion

    #region 步骤结果更新（路由系统+Handler均可调用）
    /// <summary>
    /// 记录步骤执行结果（线程安全，支持并行调用）
    /// 路由系统用：记录路由匹配步骤；Handler用：记录业务处理步骤
    /// </summary>
    public void UpdateStepResult(string stepName, bool isSuccess, TimeSpan elapsed, string? errorMsg = null, Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(stepName)) throw new ArgumentException($"[{TraceId}] 步骤名称不能为空", nameof(stepName));
        if (!isSuccess && string.IsNullOrWhiteSpace(errorMsg)) throw new ArgumentException($"[{TraceId}] 失败步骤必须提供错误信息", nameof(errorMsg));

        var result = new MqttMessageProcessStepExecuteResult
        {
            StepName = stepName,
            IsSuccess = isSuccess,
            Elapsed = elapsed,
            ErrorMsg = errorMsg,
            ExceptionDetail = exception?.ToString(),
            ExecuteTime = DateTime.UtcNow
        };
        _stepResults[stepName] = result; // ConcurrentDictionary确保线程安全
    }

    /// <summary>
    /// 获取单个步骤结果（路由系统或Handler查询）
    /// </summary>
    public MqttMessageProcessStepExecuteResult? GetStepResult(string stepName)
    {
        _stepResults.TryGetValue(stepName, out var result);
        return result;
    }

    /// <summary>
    /// 获取所有步骤结果（路由系统输出日志用）
    /// 设计：返回只读字典，避免外部修改步骤结果
    /// </summary>
    public IReadOnlyDictionary<string, MqttMessageProcessStepExecuteResult> GetAllStepResults() => _stepResults;
    #endregion

    #region 全局异常管理（路由系统或Handler记录致命错误）
    public void SetGlobalException(Exception exception)
    {
        GlobalException = exception ?? throw new ArgumentNullException(nameof(exception), $"[{TraceId}] 异常不能为null");
    }
    #endregion

    #region 日志序列化（路由系统输出日志用，生产环境监控必备）
    /// <summary>
    /// 转换为日志友好的字典（过滤敏感数据，仅包含关键元信息）
    /// 设计：避免直接序列化大对象，不暴露完整Payload，符合数据安全规范
    /// </summary>
    public Dictionary<string, object> ToLogDictionary()
    {
        var logDict = new Dictionary<string, object>
        {
            ["TraceId"] = TraceId,
            ["ReceiveTimeUtc"] = ReceiveTimeUtc.ToString("o"), // ISO 8601标准格式，便于日志分析工具解析
            ["ClientId"] = ClientId,
            ["Topic"] = Topic,
            ["ProductKey"] = ProductKey,
            ["DeviceName"] = DeviceName,
            ["IsParsedSuccess"] = IsParsedSuccess,
            ["ParseType"] = ParseType.ToString(),
            ["ParsedElapsedMs"] = ParsedElapsed.TotalMilliseconds,
            ["ParseErrorMsg"] = ParseErrorMsg ?? "无错误",
            ["IsOverallSuccess"] = IsOverallSuccess,
            ["GlobalException"] = GlobalException?.Message ?? "无全局异常",
            ["ExtensionFields"] = Extension.ToDictionary(),
            ["StepCount"] = _stepResults.Count,
            ["Steps"] = _stepResults.Values.Select(s => new
            {
                s.StepName,
                s.IsSuccess,
                ElapsedMs = s.Elapsed.TotalMilliseconds,
                s.ErrorMsg,
                ExecuteTime = s.ExecuteTime.ToString("o")
            }).ToList()
        };

        // 原始消息仅记录元数据，不暴露完整Payload（避免敏感信息泄露）
        logDict["RawMessage"] = new
        {
            RawMessage.QualityOfServiceLevel,
            RawMessage.Retain,
            PayloadLength = RawMessage.PayloadSegment.Count,
            PayloadPreview = RawMessage.GetPayloadSummary(50) // 仅展示前50字节摘要
        };

        return logDict;
    }
    #endregion

    #region 资源释放（生产环境避免内存泄漏）
    private bool _disposed = false;

    /// <summary>
    /// 释放资源（若持有非托管资源，如文件流、网络连接）
    /// 设计：遵循IDisposable标准模式，确保资源正确回收
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 释放托管资源（如Extension实现了IDisposable）
            (Extension as IDisposable)?.Dispose();
        }

        // 释放非托管资源（如原始消息中可能的非托管缓存）
        _disposed = true;
    }

    ~MqttMessageContext() => Dispose(false);
    #endregion
}

//// 你的现有关联类（复用，路由系统依赖）
//public class MqttRawMessage
//{
//    public string Topic { get; }
//    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }
//    public bool Retain { get; }
//    public ArraySegment<byte> PayloadSegment { get; }

//    public MqttRawMessage(string topic, MqttQualityOfServiceLevel qos, bool retain, ArraySegment<byte> originalPayloadSegment)
//    {
//        Topic = topic;
//        QualityOfServiceLevel = qos;
//        Retain = retain;
//        PayloadSegment = originalPayloadSegment;
//    }

//    // 你的现有方法（路由系统日志用）
//    public string GetPayloadSummary(int maxLength = 50)
//    {
//        if (PayloadSegment.Count == 0) return "Empty";
//        var payloadStr = System.Text.Encoding.UTF8.GetString(PayloadSegment.Array!, PayloadSegment.Offset, PayloadSegment.Count);
//        return payloadStr.Length <= maxLength ? payloadStr : payloadStr[..maxLength] + "...";
//    }
//}

//public enum MqttMessageDataParseType
//{
//    Unknown,
//    AlinkJson,
//    PassThrough,
//    CustomDataFomat
//}

//public class MqttMessageContextExtension
//{
//    private readonly Dictionary<string, object> _fields = new(StringComparer.OrdinalIgnoreCase);

//    public void Set(string key, object value) => _fields[key] = value;
//    public T? Get<T>(string key) => _fields.TryGetValue(key, out var value) && value is T t ? t : default;
//    public Dictionary<string, object> ToDictionary() => new(_fields);
//}