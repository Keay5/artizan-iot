using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 拓扑关系管理请求（绑定/解绑网关与子设备）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/manage-topological-relationships
/// Topic模板：/sys/${gatewayProductKey}/${gatewayDeviceName}/thing/topo/${action}
/// Method：thing.topo.${action}（action=add/delete）
/// </summary>
public class TopologyManageRequest : AlinkRequestBase
{
    /// <summary>
    /// 操作类型（add=绑定，delete=解绑）
    /// </summary>
    [JsonIgnore]
    public string Action { get; set; } = "add";

    /// <summary>
    /// 动态生成method
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => $"thing.topo.{Action}";

    /// <summary>
    /// 拓扑操作参数
    /// </summary>
    [JsonPropertyName("params")]
    public TopologyManageParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
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
        return $"/sys/{gatewayProductKey}/{gatewayDeviceName}/thing/topo/{Action}";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params.Devices == null || !Params.Devices.Any())
        {
            return ValidateResult.Failed("子设备列表不能为空");
        }
        if (Params.Devices.Count > 50)
        {
            return ValidateResult.Failed("单次最多操作50个子设备");
        }
        foreach (var device in Params.Devices)
        {
            if (string.IsNullOrWhiteSpace(device.ProductKey) || string.IsNullOrWhiteSpace(device.DeviceName))
            {
                return ValidateResult.Failed($"子设备参数非法：ProductKey/DeviceName不能为空（{device.ProductKey}/{device.DeviceName}）");
            }
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 拓扑操作参数
/// </summary>
public class TopologyManageParams
{
    [JsonPropertyName("devices")]
    public List<TopologyDeviceItem> Devices { get; set; } = new();

    [JsonPropertyName("extInfo")]
    public string ExtInfo { get; set; } = string.Empty;
}

/// <summary>
/// 拓扑设备项
/// </summary>
public class TopologyDeviceItem
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    [JsonPropertyName("signMethod")]
    public string SignMethod { get; set; } = "hmacmd5";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
