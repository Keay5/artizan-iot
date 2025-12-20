using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtt.AspNetCore;

public class IoTHubMqttAspNetcoreOptions
{
    public string IoTHubMqttServerConfigName { get; set; } = "IoTHubMqttServer";
    public string IoTHubMqttServerEndpointRouteUrl { get; set; } = "/mqtt";
}
