using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// 安全模式常量（严格遵循阿里云文档固定值）
/// </summary>
public static class MqttSecureModeConstants
{
    /// <summary>
    /// TCP 直连（不加密）
    /// </summary>
    public const int Tcp = 2;

    /// <summary>
    /// TLS 加密连接
    /// </summary>
    public const int Tls = 3;

    /// <summary>
    /// 一型一密免预注册专用（动态注册）
    /// </summary>
    public const int OneProductNoPreRegister = -2;
}
