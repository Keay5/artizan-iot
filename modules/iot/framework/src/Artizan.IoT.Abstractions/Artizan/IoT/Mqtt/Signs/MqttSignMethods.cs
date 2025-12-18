using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Signs;

/// <summary>
/// 表示签名算法类型。支持hmacmd5，hmacsha1和hmacsha256。
/// </summary>
public static class MqttSignMethods
{
    public const string Hmacsha1 = "hmacsha1";
    public const string Hmacmd5 = "hmacmd5";
    public const string HmacSha256 = "hmacsha256";  // 默认，推荐
}