using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoTHub.Products.Properties;

/// <summary>
/// 连网方式
/// </summary>
public enum ProductNetworkingModes
{
    /// <summary>
    /// Wi-Fi
    /// </summary>
    WiFi,
    /// <summary>
    /// 蜂窝（2G/3G/4G/5G）
    /// </summary>
    Cellular,
    /// <summary>
    /// 以太网
    /// </summary>
    Ethernet,
    /// <summary>
    /// LoRaWAN
    /// </summary>
    LoRaWAN,
    /// <summary>
    /// 其它
    /// </summary>
    Other
}
