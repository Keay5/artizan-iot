using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Etos;

/// <summary>
/// MQTT 事件基类   
/// </summary>

[Serializable]
public abstract class MqttEventBase
{
    public string? MqttTrackId { get; set; }

    /// <summary>
    /// the client identifier.Hint: This identifier needs to be unique over all
    /// used clients / devices on the broker to avoid connection issues.
    /// </summary>
    public string MqttClientId { get; set; }
}
