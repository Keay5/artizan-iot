using System;
using System.Security.Cryptography;
using System.Text;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// MQTT签名加密工具类（参照HmacSha256示例实现，支持多算法）
/// </summary>
public static class MqttSignCrypto
{
    /// <summary>
    /// HmacMd5 加密
    /// </summary>
    /// <param name="plainText">明文，指未经过加密、可以直接读取的原始文本或数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Ciphertext（密文，十六进制字符串，大写）</returns>
    public static string HmacMd5(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText), "明文不可为空");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "密钥不可为空");

        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// HmacSha1 加密
    /// </summary>
    /// <param name="plainText">明文，指未经过加密、可以直接读取的原始文本或数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Ciphertext（密文，十六进制字符串，大写）</returns>
    public static string HmacSha1(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText), "明文不可为空");
        }
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "密钥不可为空");
        }

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// HmacSha256 加密（与示例保持一致，补充参数校验）
    /// </summary>
    /// <param name="plainText">明文，指未经过加密、可以直接读取的原始文本或数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Ciphertext（密文，十六进制字符串，大写）</returns>
    public static string HmacSha256(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText), "明文不可为空");
        }
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "密钥不可为空");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// 根据签名算法枚举调用对应加密方法（统一入口）
    /// </summary>
    /// <param name="signMethod">签名算法类型</param>
    /// <param name="plainText">明文</param>
    /// <param name="key">密钥</param>
    /// <returns>密文（十六进制字符串，大写）</returns>
    public static string ComputeBySignMethod(MqttSignMethod signMethod, string plainText, string key)
    {
        return signMethod switch
        {
            MqttSignMethod.HmacMd5 => HmacMd5(plainText, key),
            MqttSignMethod.HmacSha1 => HmacSha1(plainText, key),
            MqttSignMethod.HmacSha256 => HmacSha256(plainText, key),
            _ => throw new NotSupportedException($"不支持的签名算法：{signMethod}")
        };
    }
}
