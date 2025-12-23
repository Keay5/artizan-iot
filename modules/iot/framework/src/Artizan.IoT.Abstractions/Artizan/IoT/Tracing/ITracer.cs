using System;

namespace Artizan.IoT.Tracing;

// 自定义追踪接口（适配你的业务）
public interface ITracer
{
    IDisposable StartSpan(string name, string traceId);
}

