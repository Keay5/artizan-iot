using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTDA.Products;

public enum ProtocolType
{
    MQTT =1, 
    LwM2MOrCoAP,
    HTTPS,
    HTTP,
    Modbus,
    OPC_UA,
    OPC_DA,
    ONVIF,
    Other = 99
}
