using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceAccess;

/// <summary>
/// 单设备登录请求（子设备上线）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/connect-or-disconnect-sub-devices
/// Topic模板：/ext/session/${productKey}/${deviceName}/combine/login
/// Method：无（协议特殊场景）
/// </summary>
public class DeviceLoginRequest : AlinkRequestBase
{
    /// <summary>
    /// 协议无method字段，忽略序列化
    /// </summary>
    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public override string Method => null!;

    /// <summary>
    /// 登录参数
    /// </summary>
    [JsonPropertyName("params")]
    public DeviceLoginParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic()
    {
        if (string.IsNullOrWhiteSpace(Params.ProductKey))
        {
            throw new ArgumentNullException(nameof(Params.ProductKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.DeviceName))
        {
            throw new ArgumentNullException(nameof(Params.DeviceName), "DeviceName不能为空");
        }
        return $"/ext/session/{Params.ProductKey}/{Params.DeviceName}/combine/login";
    }

    /// <summary>
    /// 校验参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.ClientId))
        {
            return ValidateResult.Failed("ClientId不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.Sign))
        {
            return ValidateResult.Failed("Sign不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 设备登录参数
/// </summary>
public class DeviceLoginParams
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("signMethod")]
    public string SignMethod { get; set; } = "hmacmd5";

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    [JsonPropertyName("cleanSession")]
    public bool CleanSession { get; set; } = true;
}









