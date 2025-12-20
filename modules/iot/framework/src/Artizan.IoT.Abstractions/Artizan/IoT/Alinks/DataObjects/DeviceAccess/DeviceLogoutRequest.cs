using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 子设备登出请求
/// </summary>
public class DeviceLogoutRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public override string Method => null!;

    [JsonPropertyName("params")]
    public DeviceLogoutParams Params { get; set; } = new();

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
        return $"/ext/session/{productKey}/{deviceName}/combine/logout";
    }
}

/// <summary>
/// 设备登出参数
/// </summary>
public class DeviceLogoutParams
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;
}
