using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Devices;

/// <summary>
/// 设备状态
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// 离线：设备已激活，与物联网平台断开连接
    /// </summary>
    Offline,
    /// <summary>
    /// 上线：设备已激活，与物联网平台成功连接
    /// </summary>
    Online,
}
