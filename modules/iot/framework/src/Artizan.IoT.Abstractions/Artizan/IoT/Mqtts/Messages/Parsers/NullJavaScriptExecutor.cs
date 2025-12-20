using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

public class NullJavaScriptExecutor : IJavaScriptExecutor, ITransientDependency
{
    private readonly ILogger<MqttMessageDispatcher> _logger;

    public NullJavaScriptExecutor(ILogger<MqttMessageDispatcher> logger)
    {
        _logger = logger;
    }

    public Task<string> Execute(string javaScript, byte[] rawData)
    {
        _logger.LogWarning("EmptyJavaScriptExecutor invoked. No JavaScript execution performed.");

        return Task.FromResult(string.Empty);
    }
}
