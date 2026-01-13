using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Devices;

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
