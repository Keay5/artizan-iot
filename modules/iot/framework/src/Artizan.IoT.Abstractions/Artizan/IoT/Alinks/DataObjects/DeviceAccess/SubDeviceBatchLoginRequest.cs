using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 子设备批量登录请求
/// 【协议约束】：单次最多50个子设备
/// </summary>
public class SubDeviceBatchLoginRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public override string Method => null!;

    [JsonPropertyName("params")]
    public SubDeviceBatchLoginParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic（网关设备信息）
    /// </summary>
    public string GenerateTopic(string gatewayProductKey, string gatewayDeviceName)
    {
        if (string.IsNullOrWhiteSpace(gatewayProductKey))
        {
            throw new ArgumentNullException(nameof(gatewayProductKey), "网关ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(gatewayDeviceName))
        {
            throw new ArgumentNullException(nameof(gatewayDeviceName), "网关DeviceName不能为空");
        }
        return $"/ext/session/{gatewayProductKey}/{gatewayDeviceName}/combine/batch_login";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params.DeviceList == null || !Params.DeviceList.Any())
        {
            return ValidateResult.Failed("子设备列表不能为空");
        }
        if (Params.DeviceList.Count > 50)
        {
            return ValidateResult.Failed("单次最多登录50个子设备");
        }
        foreach (var device in Params.DeviceList)
        {
            if (string.IsNullOrWhiteSpace(device.Sign))
            {
                return ValidateResult.Failed($"子设备{device.ProductKey}/{device.DeviceName}的Sign不能为空");
            }
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 批量登录参数
/// </summary>
public class SubDeviceBatchLoginParams
{
    [JsonPropertyName("deviceList")]
    public List<DeviceLoginParams> DeviceList { get; set; } = new();
}
