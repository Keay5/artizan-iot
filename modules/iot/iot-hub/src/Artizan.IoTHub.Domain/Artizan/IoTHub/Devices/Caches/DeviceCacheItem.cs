using System;

namespace Artizan.IoTHub.Devices.Caches;

[Serializable]
public class DeviceCacheItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductKey { get; set; }
    public string DeviceName { get; set; }

    /// <summary>
    /// 存储加密后的密文（缓存中实际保存的值）,
    /// 加密： StringEncryptionService.Encrypt(DeviceSecret)
    /// 解密时使用：StringEncryptionService.Decrypt(EncryptedDeviceSecret)   
    /// </summary>
    public string EncryptedDeviceSecret { get; set; }

    public string? RemarkName { get; set; }
    public bool IsActive { get; set; }
    public bool IsEnable { get; set; }
    public DeviceStatus Status { get; set; }

    public static string CalculateCacheKey(string productKey, string deviceName)
    {
        return $"pk:{productKey}:dn:{deviceName}:Device";
    }
}
