using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Metrics;

// OpenTelemetry实现（也可替换为App.Metrics/Prometheus）
public class OpenTelemetryMqttMetrics : IMqttMetrics
{
    private readonly Counter<long> _circuitBrokenCounter;
    private readonly Counter<long> _circuitResetCounter;

    public OpenTelemetryMqttMetrics(Meter meter)
    {
        // 初始化计数器
        _circuitBrokenCounter = meter.CreateCounter<long>(
            name: "mqtt.step.circuit.broken",
            description: "MQTT circuit broken counter");

        _circuitResetCounter = meter.CreateCounter<long>(
            name: "mqtt.step.circuit.reset",
            description: "MQTT circuit reset counter");
    }

    public void IncrementCounter(string counterName)
    {
        if (counterName.StartsWith("mqtt.step.circuit.broken"))
        {
            var businessLabel = counterName.Split('.').Last();
            // 方式1：显式转换value为object?
            var tag = new KeyValuePair<string, object?>("business", businessLabel);
            _circuitBrokenCounter.Add(1, tag);
        }
        else if (counterName.StartsWith("mqtt.step.circuit.reset"))
        {
            var businessLabel = counterName.Split('.').Last();
            // 方式2：直接传object?类型（更简洁）
            _circuitResetCounter.Add(1, new KeyValuePair<string, object?>("business", businessLabel));
        }
    }
}
