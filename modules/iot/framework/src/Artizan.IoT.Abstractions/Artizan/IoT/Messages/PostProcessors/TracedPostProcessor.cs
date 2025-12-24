using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 追踪基类（封装全链路追踪逻辑）
/// 设计模式：模板方法模式，基类定义追踪流程，子类实现业务逻辑。
/// </summary>
/// <typeparam name="TContext">协议上下文类型</typeparam>
public abstract class TracedPostProcessor<TContext> : IMessagePostProcessor<TContext>
    where TContext : MessageContext
{
    private readonly Tracer _tracer;

    public abstract int Priority { get; }
    public abstract bool IsEnabled { get; }

    public TracedPostProcessor(Tracer tracer)
    {
        _tracer = tracer;
    }

    /// <summary>
    /// 模板方法：先启动追踪，再执行业务逻辑
    /// </summary>
    public async Task ProcessAsync(TContext context, CancellationToken cancellationToken = default)
    {
        //TODO:重新更正使用方法
        // 创建一个 Span（替代原 ITracer 的 StartSpan）
        using var span = _tracer.StartActiveSpan(
            context.Extension.Get<string>("ProcessorName")?? GetType().Name,
            SpanKind.Internal,
            initialAttributes: new SpanAttributes(new[] { new KeyValuePair<string, object>("TraceId", context.TraceId) })
        );

        try
        {
            // 业务逻辑
            await ProcessCoreAsync(context, cancellationToken);
            span.SetStatus(Status.Ok);
        }
        catch (Exception ex)
        {
            // 记录异常
            span.RecordException(ex);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    /// <summary>
    /// 子类实现具体业务逻辑
    /// </summary>
    protected abstract Task ProcessCoreAsync(TContext context, CancellationToken cancellationToken);
}
