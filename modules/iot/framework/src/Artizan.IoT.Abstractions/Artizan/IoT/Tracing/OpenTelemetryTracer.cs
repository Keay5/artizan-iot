using OpenTelemetry.Trace;
using System;

namespace Artizan.IoT.Tracing;

public class OpenTelemetryTracer : ITracer
{
    private readonly Tracer _tracer;

    public OpenTelemetryTracer(Tracer tracer) => _tracer = tracer;

    public IDisposable StartSpan(string name, string traceId)
    {

        throw new NotImplementedException();
        //TODO:
        //return _tracer.StartActiveSpan(name, SpanKind.Internal, attributes: new[] { new KeyValuePair<string, object>("TraceId", traceId) });
    }
}
