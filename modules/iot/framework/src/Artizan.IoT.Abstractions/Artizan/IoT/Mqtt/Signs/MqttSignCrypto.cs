using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Signs;

/// <summary>
/// MQTT加密工具类
/// </summary>
public class MqttSignCrypto
{
    /// <summary>
    /// HmacSha256
    /// </summary>
    /// <param name="plainText">明文，指未经过加密、可以直接读取的原始文本或数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Ciphertext（密文）</returns>
    public static string HmacSha256(string plainText, string key)
    {
        var encoding = new UTF8Encoding();
        var plainTextBytes = encoding.GetBytes(plainText);
        var keyBytes = encoding.GetBytes(key);

        using var hmac = new HMACSHA256(keyBytes);
        var sign = hmac.ComputeHash(plainTextBytes);

        return BitConverter.ToString(sign).Replace("-", string.Empty);
    }
}
