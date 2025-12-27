using Artizan.IoT.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Artizan.IoT.Coaps.Messages;

/// <summary>
/// CoAP协议专属消息上下文（继承通用消息上下文，扩展CoAP协议特有属性）
/// 设计思想：
/// 1. 遵循开闭原则：基于抽象基类MessageContext扩展，不修改基类核心逻辑；
/// 2. 协议合规性：字段/枚举严格对齐RFC 7252（CoAP核心规范）+ RFC 8132（扩展方法）；
/// 3. 资源安全：重写Dispose模式释放CoAP特有托管资源（如Payload数组），防止内存泄漏；
/// 4. 线程安全：依赖基类MessageContextExtension的线程安全特性，支持多线程场景；
/// 5. 日志友好：重写ToLogDictionary补充CoAP特有日志字段，便于全链路问题溯源。
/// 设计模式：
/// - 模板方法模式：重写基类ToLogDictionary/Dispose方法，补充协议特有逻辑；
/// - 枚举模式：封装CoAP协议常量（方法/报文类型/状态码），避免魔法值；
/// - 防御式编程：全参数校验+已释放校验，提前暴露非法操作。
/// </summary>
public class CoapMessageContext : MessageContext
{
    #region CoAP协议核心枚举（严格对齐IETF RFC标准）
    /// <summary>
    /// CoAP请求方法（RFC 7252 §5.8 + RFC 8132扩展）
    /// </summary>
    public enum CoapMethod
    {
        /// <summary>获取资源（核心方法）</summary>
        GET = 1,
        /// <summary>创建/提交资源（核心方法）</summary>
        POST = 2,
        /// <summary>更新资源（核心方法）</summary>
        PUT = 3,
        /// <summary>删除资源（核心方法）</summary>
        DELETE = 4,
        /// <summary>获取部分资源（RFC 8132扩展）</summary>
        FETCH = 5,
        /// <summary>补丁更新资源（RFC 8132扩展）</summary>
        PATCH = 6,
        /// <summary>增量补丁更新（RFC 8132扩展）</summary>
        IPATCH = 7
    }

    /// <summary>
    /// CoAP报文类型（RFC 7252 §3.1），用于UDP可靠性保障
    /// </summary>
    public enum CoapMessageType
    {
        /// <summary>需确认报文：接收方必须回复ACK/RST，保证可靠性</summary>
        CON = 0,
        /// <summary>无需确认报文：低开销，适用于非关键数据（如温湿度上报）</summary>
        NON = 1,
        /// <summary>确认报文：对CON报文的响应确认</summary>
        ACK = 2,
        /// <summary>重置报文：接收方无法处理CON报文时的拒绝响应</summary>
        RST = 3
    }

    /// <summary>
    /// CoAP内容格式（RFC 7252 §12.3），标识Payload编码类型
    /// </summary>
    public enum CoapContentFormat
    {
        /// <summary>纯文本（UTF-8）</summary>
        TextPlain = 0,
        /// <summary>JSON格式（物联网常用）</summary>
        ApplicationJson = 50,
        /// <summary>CBOR格式（CoAP推荐二进制格式，轻量高效）</summary>
        ApplicationCbor = 60,
        /// <summary>XML格式（少用，仅兼容）</summary>
        ApplicationXml = 30,
        /// <summary>二进制流（无格式）</summary>
        ApplicationOctetStream = 42
    }

    /// <summary>
    /// CoAP响应状态码（RFC 7252 §5.9），格式：类码.原因码
    /// </summary>
    public enum CoapStatusCode
    {
        /// <summary>成功：内容返回（对应HTTP 200 OK）</summary>
        Content = 205,
        /// <summary>成功：创建完成（对应HTTP 201 Created）</summary>
        Created = 201,
        /// <summary>成功：删除完成（对应HTTP 204 No Content）</summary>
        Deleted = 202,
        /// <summary>成功：修改完成</summary>
        Changed = 204,
        /// <summary>客户端错误：资源未找到（对应HTTP 404）</summary>
        NotFound = 404,
        /// <summary>客户端错误：请求不允许（对应HTTP 405）</summary>
        MethodNotAllowed = 405,
        /// <summary>服务器错误：内部错误（对应HTTP 500）</summary>
        InternalServerError = 500,
        /// <summary>服务器错误：服务不可用（对应HTTP 503）</summary>
        ServiceUnavailable = 503
    }
    #endregion

    #region CoAP特有核心字段（只读，构造函数初始化，保证不可变）
    /// <summary>
    /// CoAP资源路径（如/temp、/led/control）
    /// 约束：非空、以/开头，符合CoAP URI规范（RFC 7252 §6.4）
    /// </summary>
    public string ResourcePath { get; }

    /// <summary>
    /// CoAP请求方法（GET/POST/PUT等）
    /// </summary>
    public CoapMethod Method { get; }

    /// <summary>
    /// CoAP报文类型（CON/NON/ACK/RST）
    /// </summary>
    public CoapMessageType MessageType { get; }

    /// <summary>
    /// CoAP消息ID（16位整数，0-65535）
    /// 作用：UDP重传/去重核心标识，同客户端+资源路径下唯一
    /// </summary>
    public ushort MessageId { get; }

    /// <summary>
    /// CoAP令牌（0-8字节）
    /// 作用：关联异步请求/响应，多请求并行时区分上下文
    /// </summary>
    public byte[]? Token { get; }

    /// <summary>
    /// CoAP负载数据（请求/响应的实际内容）
    /// 可选：如传感器数值、控制指令、错误信息
    /// </summary>
    public byte[]? Payload { get; }

    /// <summary>
    /// CoAP内容格式（标识Payload的编码类型）
    /// 可选：null表示无格式（如ACK空响应）
    /// </summary>
    public CoapContentFormat? ContentFormat { get; }

    /// <summary>
    /// CoAP响应状态码（仅响应报文有效）
    /// </summary>
    public CoapStatusCode? StatusCode { get; }

    /// <summary>
    /// CoAP观察标识（Observe机制，RFC 7641）
    /// 场景：0=订阅资源，递增数值=资源更新推送，null=未启用观察
    /// </summary>
    public uint? Observe { get; }

    /// <summary>
    /// 远端端点（客户端/服务器IP+端口）
    /// 作用：溯源请求来源，便于日志/问题排查
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// CoAP请求参数（如?temp=25&hum=60）
    /// 线程安全存储，支持多线程读写
    /// </summary>
    public MessageContextExtension QueryParameters { get; } = new MessageContextExtension();
    #endregion

    #region 资源释放标记（复用基类_disposed，防止重复释放）
    private bool _disposed = false;
    #endregion

    #region 构造函数（分层重载，适配不同场景，全参数校验）
    /// <summary>
    /// 完整构造函数（包含所有CoAP核心字段，适用于请求/响应报文）
    /// </summary>
    /// <param name="resourcePath">CoAP资源路径（如/temp）</param>
    /// <param name="method">CoAP请求方法</param>
    /// <param name="messageType">CoAP报文类型</param>
    /// <param name="messageId">CoAP消息ID（0-65535）</param>
    /// <param name="remoteEndPoint">远端端点（IP+端口）</param>
    /// <param name="clientId">客户端标识（跨协议通用）</param>
    /// <param name="token">CoAP令牌（可选，0-8字节）</param>
    /// <param name="payload">负载数据（可选）</param>
    /// <param name="contentFormat">内容格式（可选）</param>
    /// <param name="statusCode">响应状态码（仅响应报文传值，可选）</param>
    /// <param name="observe">观察标识（可选）</param>
    /// <param name="traceId">全链路追踪ID（可选，默认自动生成）</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    /// <exception cref="ArgumentNullException">必填参数为空时抛出</exception>
    /// <exception cref="ArgumentException">资源路径/令牌格式非法时抛出</exception>
    public CoapMessageContext(
        string resourcePath,
        CoapMethod method,
        CoapMessageType messageType,
        ushort messageId,
        IPEndPoint remoteEndPoint,
        string clientId,
        byte[]? token = null,
        byte[]? payload = null,
        CoapContentFormat? contentFormat = null,
        CoapStatusCode? statusCode = null,
        uint? observe = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
        : base(clientId, traceId, cancellationToken)
    {
        // 1. 核心参数非空校验
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            throw new ArgumentNullException(nameof(resourcePath), "CoAP资源路径不能为空");
        }
        if (!resourcePath.StartsWith("/"))
        {
            throw new ArgumentException("CoAP资源路径必须以/开头（如/temp），符合RFC 7252 URI规范", nameof(resourcePath));
        }
        if (remoteEndPoint == null)
        {
            throw new ArgumentNullException(nameof(remoteEndPoint), "CoAP远端端点（IP+端口）不能为空");
        }

        // 2. 令牌长度校验（CoAP规范：Token长度0-8字节，RFC 7252 §5.3）
        if (token != null && token.Length > 8)
        {
            throw new ArgumentException("CoAP令牌长度不能超过8字节（RFC 7252规范）", nameof(token));
        }

        // 3. 赋值（所有字段只读，仅构造函数初始化）
        ResourcePath = resourcePath.Trim();
        Method = method;
        MessageType = messageType;
        MessageId = messageId;
        Token = token;
        Payload = payload;
        ContentFormat = contentFormat;
        StatusCode = statusCode;
        Observe = observe;
        RemoteEndPoint = remoteEndPoint;
    }

    /// <summary>
    /// 简化构造函数（仅核心字段，适用于基础请求场景）
    /// </summary>
    /// <param name="resourcePath">CoAP资源路径</param>
    /// <param name="method">CoAP请求方法</param>
    /// <param name="clientId">客户端标识</param>
    /// <param name="remoteEndPoint">远端端点</param>
    /// <param name="traceId">全链路追踪ID（可选）</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    public CoapMessageContext(
        string resourcePath,
        CoapMethod method,
        string clientId,
        IPEndPoint remoteEndPoint,
        string? traceId = null,
        CancellationToken cancellationToken = default)
        : this(
              resourcePath: resourcePath,
              method: method,
              messageType: CoapMessageType.CON, // 默认CON（需确认），保证可靠性
              messageId: (ushort)new Random().Next(0, 65536), // 随机生成合规MessageId
              remoteEndPoint: remoteEndPoint,
              clientId: clientId,
              traceId: traceId,
              cancellationToken: cancellationToken)
    {
    }
    #endregion

    #region 重写基类方法（模板方法模式，补充CoAP特有逻辑）
    /// <summary>
    /// 扩展日志字典：合并基类通用字段 + CoAP特有字段
    /// 设计理念：结构化日志输出，便于ELK/日志平台解析，全链路溯源CoAP请求
    /// </summary>
    /// <returns>包含通用+CoAP特有字段的日志字典</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public override Dictionary<string, object> ToLogDictionary()
    {
        // 1. 基类已释放校验（防御式编程）
        CheckDisposed();

        // 2. 获取基类通用日志字段
        Dictionary<string, object> logDict = base.ToLogDictionary();

        // 3. 添加CoAP特有日志字段（前缀Coap.，便于日志过滤）
        logDict.AddOrReplayRange(new Dictionary<string, object>
        {
            ["Coap.ResourcePath"] = ResourcePath,
            ["Coap.Method"] = Method.ToString(),
            ["Coap.MessageType"] = MessageType.ToString(),
            ["Coap.MessageId"] = MessageId,
            ["Coap.Token"] = Token != null ? Convert.ToBase64String(Token) : "无", // Base64便于查看
            ["Coap.PayloadLength"] = Payload?.Length ?? 0, // 仅记录长度，避免大负载刷屏
            ["Coap.ContentFormat"] = ContentFormat?.ToString() ?? "无",
            ["Coap.StatusCode"] = StatusCode.HasValue ? StatusCode.Value.ToString() : "无",
            ["Coap.Observe"] = Observe ?? 0,
            ["Coap.RemoteEndPoint"] = $"{RemoteEndPoint.Address}:{RemoteEndPoint.Port}",
            ["Coap.QueryParametersCount"] = QueryParameters.ToDictionary().Count
        });

        return logDict;
    }

    /// <summary>
    /// 核心资源释放方法（重写基类，补充CoAP特有资源释放）
    /// 设计理念：
    /// 1. disposing=true：释放托管资源（Payload/QueryParameters）；
    /// 2. disposing=false：仅处理非托管资源（当前类无非托管资源）；
    /// 3. 调用基类Dispose保证通用资源释放，遵循“先子类后基类”释放顺序。
    /// </summary>
    /// <param name="disposing">true=手动调用Dispose()，false=GC析构函数触发</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        // ========== 1. 释放CoAP特有托管资源（仅disposing=true时执行） ==========
        if (disposing)
        {
            // 清空Payload数组，加速GC回收
            if (Payload != null)
            {
                Array.Clear(Payload, 0, Payload.Length);
            }

            // 清空Token数组
            if (Token != null)
            {
                Array.Clear(Token, 0, Token.Length);
            }

            // 释放查询参数扩展字段
            QueryParameters.Dispose();
        }

        // ========== 2. 释放CoAP特有非托管资源（无论disposing值都执行） ==========
        // 当前类基于纯托管实现，无非托管资源，预留扩展位
        // 示例：if (_unmanagedCoapSocket != IntPtr.Zero) { NativeMethods.CloseSocket(_unmanagedCoapSocket); }

        // 3. 调用基类释放逻辑（必须最后调用，保证基类资源释放）
        base.Dispose(disposing);

        // 标记已释放
        _disposed = true;
    }
    #endregion

    #region 辅助方法（提升易用性，封装CoAP特有逻辑）
    /// <summary>
    /// 将Payload转换为字符串（UTF-8编码）
    /// </summary>
    /// <returns>Payload字符串（null表示无Payload）</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public string? GetPayloadAsString()
    {
        CheckDisposed();

        if (Payload == null || Payload.Length == 0)
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(Payload);
        }
        catch (Exception ex)
        {
            return $"Payload解码失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 判断是否为响应报文（ACK/RST + 有状态码）
    /// </summary>
    /// <returns>是响应报文返回true，否则返回false</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public bool IsResponse()
    {
        CheckDisposed();

        return (MessageType == CoapMessageType.ACK || MessageType == CoapMessageType.RST)
               && StatusCode.HasValue;
    }

    /// <summary>
    /// 判断是否启用Observe观察机制
    /// </summary>
    /// <returns>启用返回true，否则返回false</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public bool IsObserved()
    {
        CheckDisposed();

        return Observe.HasValue;
    }

    /// <summary>
    /// 已释放校验（复用基类逻辑，保证一致性）
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    private new void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CoapMessageContext), "CoAP消息上下文已释放，禁止执行操作");
        }
    }
    #endregion

    #region 公共释放入口（显式实现，保证规范）
    /// <summary>
    /// 公共释放入口（符合IDisposable接口，调用核心释放逻辑）
    /// </summary>
    public new void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region 析构函数（GC兜底释放，仅处理非托管资源）
    /// <summary>
    /// 析构函数（Finalizer）：GC兜底释放非托管资源
    /// 设计理念：防止开发者未手动调用Dispose时，非托管资源泄漏
    /// </summary>
    ~CoapMessageContext()
    {
        Dispose(disposing: false);
    }
    #endregion
}
