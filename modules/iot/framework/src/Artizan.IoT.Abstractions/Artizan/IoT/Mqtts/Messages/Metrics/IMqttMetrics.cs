using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Metrics;

public interface IMqttMetrics
{
    /// <summary>
    /// 递增计数器
    /// </summary>
    /// <param name="counterName">计数器名称</param>
    void IncrementCounter(string counterName);
}
