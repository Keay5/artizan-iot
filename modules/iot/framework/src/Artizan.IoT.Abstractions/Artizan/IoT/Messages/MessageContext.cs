using Artizan.IoT.Tracing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Artizan.IoT.Messages;

/// <summary>
/// 所有协议消息的通用上下文基类
/// 设计思想：
/// 1. 抽象基类定义跨协议通用字段/行为，子类扩展协议特有逻辑（开闭原则）；
/// 2. 遵循.NET官方Dispose模式，统一管理托管/非托管资源，防止泄漏；
/// 3. 全链路追踪+UTC时间+客户端标识：保证日志/溯源的一致性；
/// 4. 线程安全：依赖MessageContextExtension的线程安全特性，支持多线程场景。
/// 设计模式：
/// - 模板方法模式：ToLogDictionary定义基础日志骨架，子类扩展协议特有字段；
/// - Dispose模式（.NET官方）：分层释放资源，区分手动/GC触发场景；
/// - 抽象工厂模式（隐含）：子类作为具体实现，基类定义通用契约。
/// ------------------------------------------------------------------------------
/// 所有协议消息的通用上下文基类（抽象层，提取共性）
/// 设计理念：
/// - 共性抽取：将TraceId、ClientId等跨协议通用字段抽象为基类，避免重复定义。
/// - 可扩展性：通过Extension字段支持业务自定义数据，无需修改类结构。
/// - 资源管理：实现IDisposable接口，规范资源释放流程。
/// 设计模式：
/// - 模板方法模式：定义ToLogDictionary等通用方法，子类可重写补充协议特有信息。
/// 
/// </summary>
public abstract class MessageContext : IHasTraceId, IDisposable
{
    #region 核心字段（跨协议通用）
    /// <summary>
    /// 全链路追踪ID
    /// protected set保护（仅子类/自身可赋值）
    /// </summary>
    public string TraceId { get; protected set; }

    /// <summary>
    /// 客户端标识（跨协议通用）
    /// </summary>
    public string ClientId { get; protected set; }

    /// <summary>
    /// 消息接收时间（UTC时间，路由系统初始化时自动赋值）
    /// 设计：统一时间基准，避免多服务器时区差异导致的时间混乱
    /// </summary>
    public DateTime ReceiveTimeUtc { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// 扩展字段（线程安全容器，支持资源释放）
    /// </summary>
    public MessageContextExtension Extension { get; } = new MessageContextExtension();

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; }
    #endregion

    #region 资源释放标记
    /// <summary>
    /// 资源释放标记（防止重复释放/访问已释放对象）
    /// </summary>
    private bool _disposed = false;
    #endregion

    #region 构造函数
    /// <summary>
    /// 受保护构造函数（抽象类禁止直接实例化）
    /// </summary>
    /// <param name="clientId">客户端标识（非空）</param>
    /// <param name="traceId">全链路追踪ID（默认自动生成无连字符GUID）</param>
    /// <param name="cancellationToken">取消令牌（默认空）</param>
    /// <exception cref="ArgumentNullException">clientId为空时抛出</exception>
    protected MessageContext(string clientId, string? traceId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentNullException(nameof(clientId), "客户端标识不能为空");
        }

        TraceId = traceId ?? Guid.NewGuid().ToString("N");
        ClientId = clientId;
        ReceiveTimeUtc = DateTime.UtcNow;
        CancellationToken = cancellationToken;
    }
    #endregion

    #region 通用业务方法（模板方法）
    /// <summary>
    /// 转换为日志字典（模板方法，子类可扩展协议特有字段）
    /// 设计理念：基础日志字段统一由基类实现，子类仅补充特有字段，降低冗余
    /// </summary>
    /// <returns>包含通用日志字段的字典</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public virtual Dictionary<string, object> ToLogDictionary()
    {
        CheckDisposed();

        Dictionary<string, object> logDict = new Dictionary<string, object>();
        {
            logDict["TraceId"] = TraceId;
            logDict["ReceiveTimeUtc"] = ReceiveTimeUtc.ToString("o"); // ISO 8601标准格式，便于日志解析
            logDict["ClientId"] = ClientId;
            logDict["ExtensionFieldsCount"] = Extension.ToDictionary().Count; // 扩展字段数量（避免日志膨胀）
        }

        return logDict;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 检查对象是否已释放，若已释放则抛出异常
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MessageContext), "消息上下文已释放，禁止执行操作");
        }
    }
    #endregion

    #region .NET 官方 Dispose 模式实现
    /// <summary>
    /// 核心资源释放方法（虚方法，子类可扩展协议特有资源释放逻辑）
    /// 设计理念：
    /// 1. 分层释放：先释放托管资源（Extension），再处理非托管资源；
    /// 2. 子类扩展：通过virtual关键字允许子类补充协议特有资源（如CoAP的Payload数组）；
    /// 3. 安全校验：释放前检查_disposed，避免重复释放。
    /// </summary>
    /// <param name="disposing">true=手动调用Dispose()，false=GC析构函数触发</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        // ========== 1. 托管资源释放（仅disposing=true时执行） ==========
        if (disposing)
        {
            // 释放核心托管资源：扩展字段容器
            Extension.Dispose();

            // 基类无其他托管资源，预留扩展位（子类可补充，如CoAP的Payload数组）
        }

        // ========== 2. 非托管资源释放（无论disposing值都执行） ==========
        // 基类基于纯托管实现，无非托管资源，预留扩展位
        // 示例：若子类引入非托管套接字句柄，可在此处释放：
        // if (_unmanagedCoapSocket != IntPtr.Zero) { NativeMethods.CloseSocket(_unmanagedCoapSocket); }

        // 标记资源已释放
        _disposed = true;
    }

    /// <summary>
    /// 公共释放入口（符合IDisposable接口规范）
    /// 设计规范：.NET官方强制要求IDisposable接口实现无参公共Dispose方法
    /// </summary>
    public void Dispose()
    {
        // 调用核心释放方法，标记为手动释放
        Dispose(disposing: true);

        // 通知GC无需调用析构函数（已手动释放所有资源，减少GC压力）
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数（GC兜底释放）
    /// 设计理念：兜底释放非托管资源，防止开发者遗漏手动Dispose导致的资源泄漏
    /// 注意：仅处理非托管资源，禁止访问Extension等托管资源（可能已被GC回收）
    /// </summary>
    ~MessageContext()
    {
        Dispose(disposing: false);
    }
    #endregion
}

