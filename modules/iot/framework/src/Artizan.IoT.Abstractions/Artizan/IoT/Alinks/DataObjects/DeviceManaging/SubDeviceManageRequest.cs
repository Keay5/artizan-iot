using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 子设备管理请求（禁用/启用/删除）
/// 【协议规范】：
/// https://help.aliyun.com/zh/iot/user-guide/enable-disable-or-delete-a-sub-device
///  适用于网关类型设备，使用该功能通知网关禁用子设备。物联网平台的云端使用异步方式推送禁用设备的消息；子设备通过网关订阅该Topic获取消息。
/// 【Topic模板】：
/// 下行：
///     - 请求：/sys/${productKey}/${deviceName}/thing/${action}
///     - 响应：/sys/${productKey}/${deviceName}/thing/${action}_reply
/// Method：thing.${action}（action=disable/enable/delete）
/// </summary>
public class SubDeviceManageRequest : AlinkRequestBase
{
    /// <summary>
    /// 操作类型（disable=禁用，enable=启用，delete=删除）
    /// </summary>
    [JsonIgnore]
    public string Action { get; set; } = "disable";

    /// <summary>
    /// 动态生成method
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => $"thing.{Action}";

    /// <summary>
    /// 子设备管理参数 //TODO:？ 协议中说：params	Object	请求参数， 为空即可。待考虑是否删除
    /// </summary>
    [JsonPropertyName("params")]
    public SubDeviceManageParams Params { get; set; } = new();

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
        return $"/sys/{gatewayProductKey}/{gatewayDeviceName}/thing/{Action}";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.ProductKey))
        {
            return ValidateResult.Failed("ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.DeviceName))
        {
            return ValidateResult.Failed("DeviceName不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 子设备管理参数
/// </summary>
public class SubDeviceManageParams
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("extInfo")]
    public string ExtInfo { get; set; } = string.Empty;
}

