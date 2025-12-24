using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.Properties;

/// <summary>
/// 设备安全认证：
///     https://help.aliyun.com/zh/iot/user-guide/authenticate-devices/?spm=a2c4g.11186623.help-menu-30520.d_2_2_1_1.634d2a48Xx4GNK
/// 认证方式：
/// - 如何使用设备密钥进行设备身份认证 立即了解
///   https://help.aliyun.com/zh/iot/user-guide/overview-1?spm=5176.11485173.help.dexternal.705f59afDq1Fkn
///     
/// - 如何购买并使用 ID² 进行设备身份认证 立即了解
///   https://help.aliyun.com/zh/iot-device-id/getting-started/novice-guide?spm=5176.11485173.help.dexternal.705f59afDq1Fkn
/// 
/// - 如何使用 X.509 证书进行设备身份认证 立即了解
///   https://help.aliyun.com/zh/iot/user-guide/use-x-509-certificates-to-verify-devices?spm=5176.11485173.help.dexternal.705f59afDq1Fkn
/// </summary>
public enum ProductAuthenticationMode
{
    /// <summary>
    /// 设备密钥
    /// </summary>
    DeviceSecret,
    /// <summary>
    /// X.509 证书
    /// </summary>
    X509Certificate
}
