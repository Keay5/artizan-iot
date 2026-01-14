using Artizan.IoT.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Messages;

/// <summary>
/// MQTT消息处理上下文（全流程唯一数据载体，核心领域对象）
/// 设计思想：
/// 1. 领域驱动设计（DDD）：聚合MQTT消息全生命周期数据，作为核心领域对象贯穿路由/解析/执行全流程；
/// 2. 开闭原则：通过Extension扩展字段支持业务自定义，无需修改类结构；
/// 3. 线程安全：核心步骤容器采用ConcurrentDictionary，适配多线程并行处理场景；
/// 4. 不可变核心：TraceId/ClientId/Topic等核心标识初始化后不可修改，避免数据篡改；
/// 5. Fail-Fast：严格参数校验，提前暴露非法输入，降低下游故障概率；
/// 6. .NET 官方规范：遵循IDisposable标准模式、命名规范、异常处理规范。
/// 7. 可追踪：内置TraceId+时间戳+耗时统计，全链路问题可排查；
/// 8. 扩展性：Extension字段支持业务自定义数据，无需修改类结构。
/// 设计模式：
/// - 容器模式：聚合消息全生命周期数据，替代多参数传递；
/// - 状态模式：通过IsParsedSuccess/IsOverallSuccess管理上下文状态；
/// - 线程安全模式：ConcurrentDictionary保障多线程更新安全；
/// - Dispose模式（.NET官方）：分层释放托管/非托管资源，防止内存泄漏。
/// 路由系统适配流程：
/// 1. 初始化：路由系统接收消息后调用基础构造函数创建实例；
/// 2. 路由匹配：填充TopicPlaceholderValues、ProductKey、DeviceName；
/// 3. 业务处理：Handler调用SetParseSuccess/SetParseFailed更新解析状态，调用UpdateStepResult记录步骤；
/// 4. 结果输出：路由系统通过IsOverallSuccess判断状态，ToLogDictionary输出结构化日志；
/// 5. 资源释放：使用using语句或手动Dispose释放资源，避免内存泄漏。
/// ---------------------------------------------------------------------------------------------------
/// MQTT协议消息上下文（继承通用基类，扩展MQTT特有属性）
/// 设计理念：
/// - 协议特性保留：封装Topic、RawMessage等MQTT特有字段，与通用字段分离。
/// - 状态管理：通过IsParsedSuccess、StepResults等跟踪消息处理状态。
/// - 线程安全：核心容器采用ConcurrentDictionary，支持多线程并行更新。
/// 设计模式：
/// - 状态模式：通过IsParsedSuccess、IsOverallSuccess管理处理状态，简化流程判断。
/// - 容器模式：聚合MQTT消息全生命周期数据，替代多参数传递。
/// </summary>
public class MqttMessageContext : MessageContext
{
    #region 基础层（路由系统初始化+填充，核心标识不可变）
    /// <summary>
    /// 原始MQTT消息（路由系统接收后封装，仅引用不拷贝，降低内存占用）
    /// 设计约束：
    /// - 只读：初始化后不可修改，避免原始消息被篡改；
    /// - 轻量：仅持有引用，不拷贝Payload，适配大消息场景。
    /// </summary>
    public MqttRawMessage RawMessage { get; }

    /// <summary>
    /// MQTT Topic（从RawMessage提取，路由匹配核心字段）
    /// 设计：冗余存储，避免频繁从RawMessage读取，提升路由匹配效率。
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Topic解析的占位符值（如productKey/deviceName/tsl.event.identifier）
    /// 设计：路由系统自动填充，Handler直接使用，避免重复解析Topic；
    /// 示例：Topic="/sys/pk123/dn456/event" → {"productKey":"pk123", "deviceName":"dn456"}。
    /// </summary>
    public Dictionary<string, string> TopicPlaceholderValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 产品唯一标识（路由系统从占位符提取，核心业务字段）
    /// 设计：独立存储，简化Handler访问，避免重复从字典取值。
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备唯一标识（路由系统从占位符提取，核心业务字段）
    /// 设计：同ProductKey，提升业务代码可读性和访问效率。
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    #endregion

    #region 解析层（Handler自主更新，路由系统仅读取）
    /// <summary>
    /// 消息数据解析类型（标记ParsedData的数据格式，避免类型转换错误）
    /// 设计：与ParsedData配合，避免类型转换错误
    /// </summary>
    public MessageDataParseType DataParseType { get; private set; } = MessageDataParseType.Unknown;

    /// <summary>
    /// 解析后的结构化数据（Handler更新，路由系统不干预解析逻辑）
    /// 设计：object类型避免泛型导致的类膨胀，通过ParseType约束实际类型。
    /// </summary>
    public object? ParsedData { get; private set; }

    /// <summary>
    /// 解析是否成功（Handler更新，路由系统判断是否继续执行后续步骤）
    /// 设计：简化路由系统逻辑，无需通过异常间接判断解析状态
    /// </summary>
    public bool IsParsedSuccess { get; private set; }

    /// <summary>
    /// 解析耗时（Handler更新，用于性能监控和瓶颈排查）
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
    /// 线程安全的步骤执行结果容器（支持多线程并行更新）
    /// 设计：
    /// - ConcurrentDictionary：保障多线程Add/Update安全；
    /// - 私有字段：仅通过公共方法更新，避免外部直接修改。
    /// </summary>
    private readonly ConcurrentDictionary<string, MessageProcessResult> _processResults = new();

    /// <summary>
    /// 整体执行是否成功（解析成功 + 所有步骤成功）
    /// 设计：只读计算属性，简化上游系统结果判断。
    /// </summary>
    public bool IsOverallSuccess => IsParsedSuccess && _processResults.Values.All(s => s.IsSuccess);

    /// <summary>
    /// 全局致命异常（中断整个流程的错误，如路由失败、解析崩溃）
    /// 设计：区分步骤异常和流程异常，便于定位致命问题。
    /// </summary>
    public Exception? GlobalException { get; private set; }
    #endregion

    #region 资源释放标记
    /// <summary>
    /// 资源释放标记（防止重复释放/访问已释放对象）
    /// 设计：线程安全（volatile），适配多线程释放场景。
    /// </summary>
    private volatile bool _disposed = false;
    #endregion

    #region 构造函数（路由系统初始化调用，严格参数校验）
    /// <summary>
    /// 基础构造函数（路由匹配前调用，未解析ProductKey/DeviceName）
    /// 场景：消息刚接收，仅需基础Topic和Payload，路由匹配前初始化。
    /// </summary>
    /// <param name="mqttRawMessage">原始MQTT消息（非空）</param>
    /// <param name="mqttClientId">客户端ID（非空）</param>
    /// <param name="traceId">全链路追踪ID（默认自动生成）</param>
    /// <param name="cancellationToken">取消令牌（默认None）</param>
    /// <exception cref="ArgumentNullException">必填参数为空时抛出</exception>
    public MqttMessageContext(
        MqttRawMessage mqttRawMessage,
        string mqttClientId,
        string? traceId = null,
        CancellationToken cancellationToken = default)
        : base(mqttClientId, traceId, cancellationToken) // 调用基类构造函数初始化通用字段
    {
        // 严格参数校验（Fail-Fast原则）
        RawMessage = mqttRawMessage ?? throw new ArgumentNullException(
            nameof(mqttRawMessage),
            $"[{TraceId}] 原始MQTT消息不能为空");

        if (string.IsNullOrWhiteSpace(mqttRawMessage.Topic))
        {
            throw new ArgumentNullException(
                nameof(mqttRawMessage.Topic),
                $"[{TraceId}] MQTT Topic不能为空");
        }

        // 赋值核心不可变字段
        Topic = mqttRawMessage.Topic;
    }

    /// <summary>
    /// 兼容构造函数（路由匹配后调用，已解析ProductKey/DeviceName）
    /// 过时说明：推荐使用基础构造函数 + TopicPlaceholderValues赋值，降低构造函数复杂度。
    /// </summary>
    /// <param name="mqttRawMessage">原始MQTT消息</param>
    /// <param name="mqttClientId">客户端ID</param>
    /// <param name="productKey">产品标识</param>
    /// <param name="deviceName">设备标识</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public MqttMessageContext(
        MqttRawMessage mqttRawMessage,
        string mqttClientId,
        string productKey,
        string deviceName,
        string? traceId = null,
        CancellationToken cancellationToken = default)
        : this(mqttRawMessage, mqttClientId, traceId, cancellationToken)
    {
        // 产品/设备标识校验
        ProductKey = productKey ?? throw new ArgumentNullException(
            nameof(productKey),
            $"[{TraceId}] 产品Key不能为空");

        DeviceName = deviceName ?? throw new ArgumentNullException(
            nameof(deviceName),
            $"[{TraceId}] 设备名称不能为空");
    }
    #endregion

    #region 解析结果更新（Handler调用，防御式校验）
    /// <summary>
    /// 标记解析成功（Handler解析完成后调用）
    /// 设计约束：仅允许调用一次，避免重复设置解析结果。
    /// </summary>
    /// <param name="parseType">解析类型</param>
    /// <param name="parsedData">解析后的结构化数据（非空）</param>
    /// <param name="elapsed">解析耗时</param>
    /// <exception cref="InvalidOperationException">重复调用时抛出</exception>
    /// <exception cref="ArgumentNullException">parsedData为空时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void SetParseSuccess(MessageDataParseType parseType, object parsedData, TimeSpan elapsed)
    {
        CheckDisposed();

        if (IsParsedSuccess)
        {
            throw new InvalidOperationException($"[{TraceId}] 解析结果已设置，不允许重复调用SetParseSuccess");
        }

        ParsedData = parsedData ?? throw new ArgumentNullException(
            nameof(parsedData),
            $"[{TraceId}] 解析数据不能为空");

        DataParseType = parseType;
        IsParsedSuccess = true;
        ParsedElapsed = elapsed;
        ParseErrorMsg = null;
    }

    /// <summary>
    /// 标记解析失败（Handler解析失败后调用）
    /// 设计：记录错误信息+异常，路由系统据此终止流程。
    /// </summary>
    /// <param name="errorMsg">错误信息（非空）</param>
    /// <param name="elapsed">解析耗时</param>
    /// <param name="exception">关联异常（可选）</param>
    /// <exception cref="InvalidOperationException">解析已成功时抛出</exception>
    /// <exception cref="ArgumentException">errorMsg为空时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void SetParseFailed(string errorMsg, TimeSpan elapsed, Exception? exception = null)
    {
        CheckDisposed();

        if (IsParsedSuccess)
        {
            throw new InvalidOperationException($"[{TraceId}] 解析已成功，无法调用SetParseFailed标记为失败");
        }

        if (string.IsNullOrWhiteSpace(errorMsg))
        {
            errorMsg = $"[{TraceId}] 解析失败（无详细错误信息）";
        }
        else
        {
            errorMsg = $"[{TraceId}] {errorMsg}" + (exception != null ? $" | 异常详情: {exception.Message}" : string.Empty);
        }

        DataParseType = MessageDataParseType.Unknown;
        ParsedData = null;
        IsParsedSuccess = false;
        ParsedElapsed = elapsed;
        ParseErrorMsg = errorMsg;
        GlobalException = exception; // 解析失败视为全局异常，终止流程
    }

    #endregion

    #region 步骤结果更新（线程安全，路由+Handler共用）
    /// <summary>
    /// 记录步骤执行结果（线程安全，支持并行调用）
    /// </summary>
    /// <param name="processName">步骤名称（非空）</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="elapsed">步骤耗时</param>
    /// <param name="errorMsg">错误信息（失败时非空）</param>
    /// <param name="exception">关联异常（可选）</param>
    /// <exception cref="ArgumentException">stepName/errorMsg为空时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void UpdateProcessResult(string processName, bool isSuccess, TimeSpan elapsed, string? errorMsg = null, Exception? exception = null)
    {
        CheckDisposed();

        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException($"[{TraceId}] 步骤名称不能为空", nameof(processName));
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(errorMsg))
        {
            throw new ArgumentException($"[{TraceId}] 失败步骤[{processName}]必须提供错误信息", nameof(errorMsg));
        }

        // 构建步骤结果（UTC时间戳，统一时间基准）
        var processResult = new MessageProcessResult
        {
            ProcessName = processName,
            IsSuccess = isSuccess,
            Elapsed = elapsed,
            ErrorMsg = errorMsg,
            ExceptionDetail = exception?.ToString(),
            ExecuteTime = DateTime.UtcNow
        };

        // ConcurrentDictionary原子更新，保障线程安全
        _processResults[processName] = processResult;
    }

    /// <summary>
    /// 获取单个步骤结果（查询用）
    /// </summary>
    /// <param name="stepName">步骤名称</param>
    /// <returns>步骤结果（null表示未找到）</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public MessageProcessResult? GetProcessResult(string stepName)
    {
        CheckDisposed();

        if (string.IsNullOrWhiteSpace(stepName))
        {
            return null;
        }

        _processResults.TryGetValue(stepName, out var result);
        return result;
    }

    /// <summary>
    /// 获取所有步骤结果（只读，避免外部修改）
    /// </summary>
    /// <returns>只读步骤结果字典</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public IReadOnlyDictionary<string, MessageProcessResult> GetAllProcessResults()
    {
        CheckDisposed();
        return _processResults;
    }
    #endregion

    #region 全局异常管理
    /// <summary>
    /// 记录全局致命异常（中断整个流程）
    /// </summary>
    /// <param name="exception">异常实例（非空）</param>
    /// <exception cref="ArgumentNullException">exception为空时抛出</exception>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void SetGlobalException(Exception exception)
    {
        CheckDisposed();

        GlobalException = exception ?? throw new ArgumentNullException(
            nameof(exception),
            $"[{TraceId}] 全局异常不能为空");
    }
    #endregion

    #region 日志序列化（结构化输出，安全合规）
    /// <summary>
    /// 转换为日志友好的字典（过滤敏感数据，仅包含关键元信息）
    /// 设计原则：
    /// - 安全：不暴露完整Payload，仅输出前50字节摘要；
    /// - 结构化：字段命名规范，便于ELK/日志平台解析；
    /// - 轻量化：避免序列化大对象，降低日志存储开销。
    /// </summary>
    /// <returns>结构化日志字典</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public override Dictionary<string, object> ToLogDictionary()
    {
        CheckDisposed();

        // 1. 获取基类通用日志字段
        Dictionary<string, object> logDict = base.ToLogDictionary();

        // 2. 添加MQTT特有核心字段
        logDict.AddOrReplayRange(new Dictionary<string, object>
        {
            ["Topic"] = Topic,
            ["ProductKey"] = ProductKey,
            ["DeviceName"] = DeviceName,
            ["IsParsedSuccess"] = IsParsedSuccess,
            ["ParseType"] = DataParseType.ToString(),
            ["ParsedElapsedMs"] = ParsedElapsed.TotalMilliseconds,
            ["ParseErrorMsg"] = ParseErrorMsg ?? "无错误",
            ["IsOverallSuccess"] = IsOverallSuccess,
            ["GlobalException"] = GlobalException?.Message ?? "无全局异常",
            ["ExtensionFields"] = Extension.ToDictionary(),
            ["StepCount"] = _processResults.Count,
            ["Steps"] = _processResults.Values.Select(s => new
            {
                s.ProcessName,
                s.IsSuccess,
                ElapsedMs = s.Elapsed.TotalMilliseconds,
                ErrorMsg = s.ErrorMsg ?? "无",
                ExecuteTime = s.ExecuteTime.ToString("o") // ISO 8601 UTC时间
            }).ToList()
        });

        // 3. 原始消息元数据（仅输出摘要，避免敏感信息泄露）
        logDict["Mqtt.RawMessage"] = new
        {
            QualityOfServiceLevel = RawMessage.QualityOfServiceLevel.ToString(),
            RawMessage.Retain,
            PayloadLength = RawMessage.PayloadSegment.Count,
            PayloadPreview = RawMessage.GetPayloadSummary(50) // 仅展示前50字节
        };

        return logDict;
    }
    #endregion

    #region 资源释放（.NET官方Dispose模式）
    /// <summary>
    /// 检查对象是否已释放，若已释放则抛出异常
    /// 设计：防御式编程，提前暴露非法操作，避免隐藏bug。
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(MqttMessageContext),
                $"[{TraceId}] MQTT消息上下文已释放，禁止执行操作");
        }
    }

    /// <summary>
    /// 核心资源释放方法（重写基类，释放子类特有资源）
    /// 设计：
    /// - disposing=true：释放托管资源（步骤容器、扩展字段）；
    /// - disposing=false：仅释放非托管资源（当前类无非托管资源）；
    /// - 调用基类Dispose，保证通用资源释放。
    /// </summary>
    /// <param name="disposing">true=手动释放，false=GC兜底释放</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        // 1. 释放托管资源（仅手动释放时执行）
        if (disposing)
        {
            // 清空步骤容器，加速GC回收
            _processResults.Clear();

            // 释放基类扩展字段（已实现IDisposable）
            Extension.Dispose();
        }

        // 2. 释放非托管资源（当前类无非托管资源，预留扩展位）
        // 示例：若持有非托管句柄，在此处释放：
        // if (_unmanagedBuffer != IntPtr.Zero) { NativeMethods.FreeBuffer(_unmanagedBuffer); }

        // 3. 标记已释放
        _disposed = true;

        // 4. 调用基类释放逻辑（必须最后调用）
        base.Dispose(disposing);
    }

    /// <summary>
    /// 公共释放入口（符合IDisposable接口规范）
    /// 设计：对外暴露统一释放接口，支持using语句。
    /// </summary>
    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // 告诉GC无需调用析构函数
    }

    /// <summary>
    /// 析构函数（GC兜底释放非托管资源）
    /// 设计：防止开发者未手动Dispose时，非托管资源泄漏。
    /// </summary>
    ~MqttMessageContext()
    {
        Dispose(false);
    }
    #endregion
}
