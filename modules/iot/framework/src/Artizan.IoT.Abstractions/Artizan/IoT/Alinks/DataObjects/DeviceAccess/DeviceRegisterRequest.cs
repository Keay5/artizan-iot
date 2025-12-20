using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 设备身份注册请求（Alink协议：设备接入场景）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/register-devices
/// Topic模板：/sys/${productKey}/${deviceName}/thing/device/register
/// Method：thing.device.register
/// </summary>
public class DeviceRegisterRequest : AlinkRequestBase
{
    /// <summary>
    /// 固定method（协议约束）
    /// </summary>
    [JsonPropertyName("method")]
    public override string Method => "thing.device.register";

    /// <summary>
    /// 注册参数
    /// </summary>
    [JsonPropertyName("params")]
    public DeviceRegisterParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/device/register";
    }

    /// <summary>
    /// 校验注册参数合法性
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
        if (string.IsNullOrWhiteSpace(Params.DeviceSecret))
        {
            return ValidateResult.Failed("DeviceSecret不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 设备注册参数
/// </summary>
public class DeviceRegisterParams
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("deviceSecret")]
    public string DeviceSecret { get; set; } = string.Empty;

    /// <summary>
    /// 由设备上传
    /// </summary>
    [JsonPropertyName("random")]
    public string Random { get; set; } = string.Empty;//= Random.Shared.Next(100000, 999999).ToString();

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    [JsonPropertyName("signMethod")]
    public string SignMethod { get; set; } = "hmacsha1";
}
