using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Options;

public class IoTMqttServerOptions
{
    public string IpAddress { get; set; }
    public string DomainName { get; set; }
    public int Port { get; set; } = 1883;
    public bool EnableTls { get; set; } = false;
    public int TlsPort { get; set; } = 8883;
    public int WebSocketPort { get; set; } = 5883;
}

