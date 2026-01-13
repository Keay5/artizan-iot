using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoTHub.Products.Properties;

/// <summary>
/// 接入网关协议
/// </summary>
public enum ProductAccessGatewayProtocol
{
    Custom,
    Modbus,
    OPCUA,
    ZigBee,
    BLE
}
