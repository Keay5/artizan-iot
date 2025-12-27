using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备网络状态上报请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-network-status
/// Method：thing.device.network.status.post
/// </summary>
public class DeviceNetworkStatusPostRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.device.network.status.post";

    [JsonPropertyName("params")]
    public DeviceNetworkStatusParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/device/network/status/post";
    }
}

/// <summary>
/// 网络状态参数
/// </summary>
public class DeviceNetworkStatusParams
{
    [JsonPropertyName("networkType")]
    public string NetworkType { get; set; } = string.Empty; // 2g/3g/4g/wifi/ethernet

    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; } // 信号强度（0-100）

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}


