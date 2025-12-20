using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备分发请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-distribution
/// Method：thing.device.distribute
/// </summary>
public class DeviceDistributionRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.device.distribute";

    [JsonPropertyName("params")]
    public DeviceDistributionParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/device/distribute";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.TargetProductKey))
        {
            return ValidateResult.Failed("目标ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.TargetDeviceName))
        {
            return ValidateResult.Failed("目标DeviceName不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 设备分发参数
/// </summary>
public class DeviceDistributionParams
{
    [JsonPropertyName("targetProductKey")]
    public string TargetProductKey { get; set; } = string.Empty;

    [JsonPropertyName("targetDeviceName")]
    public string TargetDeviceName { get; set; } = string.Empty;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    [JsonPropertyName("signMethod")]
    public string SignMethod { get; set; } = "hmacmd5";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

