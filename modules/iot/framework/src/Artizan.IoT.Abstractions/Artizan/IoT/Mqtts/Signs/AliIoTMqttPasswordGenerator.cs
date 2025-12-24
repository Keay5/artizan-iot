using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Signs;

public class AliIoTMqttPasswordGenerator
{
    public enum SignMethod
    {
        HmacMd5,
        HmacSha1
    }

    public string GenerateClientId(string clientId, string productKey, string deviceName, long timestamp, SignMethod signMethod)
    {
        return $"{clientId}|securemode=2,signmethod={GetSignMethodString(signMethod)},timestamp={timestamp}|";
    }

    public string GenerateUsername(string deviceName, string productKey)
    {
        return $"{deviceName}&{productKey}";
    }

    public string GeneratePassword(string productKey, string deviceName, string deviceSecret, string clientId, long? timestamp, SignMethod signMethod)
    {
        var parameters = new SortedDictionary<string, string>
            {
                {"productKey", productKey},
                {"deviceName", deviceName},
                {"clientId", clientId}
            };

        if (timestamp.HasValue)
        {
            parameters.Add("timestamp", timestamp.Value.ToString());
        }

        var content = string.Join("", parameters.Select(kv => $"{kv.Key}{kv.Value}"));

        return signMethod switch
        {
            SignMethod.HmacMd5 => ComputeHmacMd5(deviceSecret, content),
            SignMethod.HmacSha1 => ComputeHmacSha1(deviceSecret, content),
            _ => throw new ArgumentOutOfRangeException(nameof(signMethod), "不支持的签名方法")
        };
    }

    private string ComputeHmacMd5(string key, string content)
    {
        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    private string ComputeHmacSha1(string key, string content)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
    }

    private string GetSignMethodString(SignMethod signMethod)
    {
        return signMethod switch
        {
            SignMethod.HmacMd5 => "hmacmd5",
            SignMethod.HmacSha1 => "hmacsha1",
            _ => throw new ArgumentOutOfRangeException(nameof(signMethod))
        };
    }
}