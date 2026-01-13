using System;
using System.Collections.Generic;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// 一机一密签名与认证工具（独立实现，严格遵循阿里云规范）
/// 核心特性：
/// 1. ClientId格式：ProductKey.DeviceName|参数|
/// 2. 安全模式：支持2（TCP）/3（TLS）
/// 3. 签名原文：clientId + deviceName + productKey + timestamp（ASCII升序拼接）
/// </summary>
public class OneDeviceOneSecretMqttSign
{
    /// <summary>
    /// 生成MQTT连接参数（设备端使用）
    /// </summary>
    /// <param name="productKey">产品Key（设备预烧录）</param>
    /// <param name="deviceName">设备名称（设备预烧录）</param>
    /// <param name="deviceSecret">设备密钥（设备预烧录）</param>
    /// <param name="signMethod">签名算法</param>
    /// <param name="secureMode">安全模式（2=TCP，3=TLS）</param>
    /// <param name="timestamp">时间戳（毫秒级，防止重放攻击）</param>
    /// <param name="instanceId">企业版实例ID（可选）</param>
    /// <returns>ClientId/UserName/Password/认证参数</returns>
    public (string MqttClientId, string MqttUserName, string MqttPassword, MqttAuthParams Params) GenerateConnectParams(
        string productKey,
        string deviceName,
        string deviceSecret,
        MqttSignMethod signMethod = MqttSignMethod.HmacSha256,
        int? secureMode = null,
        string? timestamp = null,
        string? random = null,
        string? instanceId = null)
    {
        // 1. 基础参数合法性校验
        ValidateBaseParams(productKey, deviceName, deviceSecret);

        // 2. 安全模式校验（仅支持2/3）
        var actualSecureMode = secureMode ?? MqttAuthType.OneDeviceOneSecret.GetFixedSecureMode();
        ValidateSecureMode(actualSecureMode);

        // 3. 构建认证参数
        var authParams = BuildAuthParams(productKey, deviceName, signMethod, actualSecureMode, random, timestamp, instanceId);

        // 4. 生成各连接参数
        var clientId = BuildMqttClientId(authParams);
        var userName = BuildMqttUserName(productKey, deviceName);
        var password = GenerateMqttPassword(authParams, deviceSecret);

        return (clientId, userName, password, authParams);
    }

    /// <summary>
    /// 验证MQTT签名（服务端使用）
    /// </summary>
    /// <param name="mqttClientId">客户端提交的ClientId</param>
    /// <param name="mqttUserName">客户端提交的UserName</param>
    /// <param name="mqttPassword">客户端提交的Password（签名）</param>
    /// <param name="deviceSecret">服务端存储的设备密钥</param>
    /// <returns>认证结果</returns>
    public MqttAuthResult ValidateSign(string mqttClientId, string mqttUserName, string mqttPassword, string deviceSecret, MqttAuthParams? authParams = null)
    {
        try
        {
            // 1. 解析ClientId获取基础参数
            authParams ??= MqttSignUtils.ParseMqttClientIdAndUserName(mqttClientId, mqttUserName);
            if (authParams == null)
            {
                return MqttAuthResult.Fail(MqttAuthErrorCode.ClientIdFormatInvalid, "ClientId格式非法（需符合：ProductKey.DeviceName|authType=xxx,...|）");
            }

            // 2. 验证认证类型匹配
            if (authParams.AuthType != MqttAuthType.OneDeviceOneSecret)
            {
                return MqttAuthResult.Fail(MqttAuthErrorCode.AuthTypeMismatch, $"认证类型不匹配，预期：{MqttAuthType.OneDeviceOneSecret}");
            }

            // 3. 核心参数完整性校验
            var validateMsg = MqttSignUtils.ValidateCoreParams(authParams);
            if (!string.IsNullOrEmpty(validateMsg))
            {
                return MqttAuthResult.Fail(MqttAuthErrorCode.AuthParamsInvalid, validateMsg);
            }

            // 4. ClientId格式校验（必须包含ProductKey前缀）
            if (!mqttClientId.StartsWith($"{authParams.ProductKey}."))
            {
                return MqttAuthResult.Fail(MqttAuthErrorCode.ClientIdFormatInvalid, "ClientId前缀格式非法（需符合：ProductKey.DeviceName）");
            }

            // 5. 安全模式合法性校验
            ValidateSecureMode(authParams.SecureMode);

            // 6. 重新计算签名并比对
            var expectedPassword = GenerateMqttPassword(authParams, deviceSecret);
            if (expectedPassword != mqttPassword)
            {
                return MqttAuthResult.Fail(MqttAuthErrorCode.SignatureVerifyFailed, "签名验证失败（Password不匹配）");
            }

            // 8. 认证通过
            return MqttAuthResult.Success(authParams);
        }
        catch (Exception ex)
        {
            return MqttAuthResult.Fail(MqttAuthErrorCode.ServerError, $"认证系统异常：{ex.Message}");
        }
    }

    #region 私有方法
    /// <summary>
    /// 验证基础参数合法性
    /// </summary>
    private void ValidateBaseParams(string productKey, string deviceName, string deviceSecret)
    {
        if (!MqttSignUtils.IsValidProductKey(productKey))
        {
            throw new ArgumentException("ProductKey非法：长度4-30字符，支持字母、数字、_、-、@、()");
        }

        if (!MqttSignUtils.IsValidDeviceName(deviceName))
        {
            throw new ArgumentException("DeviceName非法：长度4-32字符，支持字母、数字、_、-、@、.、:");
        }

        if (string.IsNullOrWhiteSpace(deviceSecret))
        {
            throw new ArgumentNullException(nameof(deviceSecret), "DeviceSecret不能为空");
        }
    }

    /// <summary>
    /// 验证安全模式合法性（仅支持2/3）
    /// </summary>
    private void ValidateSecureMode(int secureMode)
    {
        if (secureMode != MqttSecureModeConstants.Tcp && secureMode != MqttSecureModeConstants.Tls)
        {
            throw new ArgumentException($"一机一密安全模式仅支持 TCP 或 TLS 验证安全模式", nameof(secureMode));
        }
    }

    /// <summary>
    /// 构建认证参数对象
    /// </summary>
    private MqttAuthParams BuildAuthParams(string productKey, string deviceName, MqttSignMethod signMethod, int secureMode, string? random, string? timestamp, string? instanceId)
    {
        return new MqttAuthParams
        {
            AuthType = MqttAuthType.OneDeviceOneSecret,
            SecureMode = secureMode,
            SignMethod = signMethod,
            ProductKey = productKey,
            DeviceName = deviceName,
            Random = random,
            Timestamp = timestamp,
            InstanceId = instanceId
        };
    }

    /// <summary>
    /// 构建ClientId（一机一密格式）
    /// </summary>
    private string BuildMqttClientId(MqttAuthParams param)
    {
        var paramList = new List<string>
        {
            $"authType={(int)param.AuthType}",
            $"secureMode={param.SecureMode}",
            $"signMethod={param.SignMethod.ToString()}"
        };

        if (!string.IsNullOrWhiteSpace(param.Random))
        {
            paramList.Add($"random={param.Random}");
        }

        if (!string.IsNullOrWhiteSpace(param.Timestamp))
        {
            paramList.Add($"timestamp={param.Timestamp}");
        }

        if (!string.IsNullOrWhiteSpace(param.InstanceId))
        {
            paramList.Add($"instanceId={param.InstanceId}");
        }

        // 格式：ProductKey.DeviceName|参数1,参数2,...|
        return $"{param.ProductKey}.{param.DeviceName}|{string.Join(",", paramList)}|";
    }

    /// <summary>
    /// 构建UserName（格式：DeviceName&ProductKey）
    /// </summary>
    private string BuildMqttUserName(string productKey, string deviceName)
    {
        return $"{deviceName}&{productKey}";
    }

    /// <summary>
    /// 生成签名（Password）
    /// 签名原文：按ASCII升序排列 clientId、deviceName、productKey、timestamp 并拼接
    /// </summary>
    private string GenerateMqttPassword(MqttAuthParams param, string deviceSecret)
    {
        // 按ASCII升序排列参数（clientId < deviceName < productKey < random <timestamp）
        var signParams = new SortedDictionary<string, string>
        {
            // 注意：1.一机一密:clientId=ProductKey.DeviceName；2.此处的clientId不是 MqttClientId。
            { "clientId", $"{param.ProductKey}.{param.DeviceName}" },
            { "deviceName", param.DeviceName },
            { "productKey", param.ProductKey },
            { "random", param.Random??"" },
            { "timestamp", param.Timestamp??"" }
        };

        // 先保证排序，若空则再删除
        if (string.IsNullOrWhiteSpace(param.Random))
        {
            signParams.Remove("random");
        }

        // 先保证排序，若空则再删除
        if (string.IsNullOrWhiteSpace(param.Timestamp))
        {
            signParams.Remove("timestamp");
        }

        // 拼接签名原文（注意：无分割符）
        var plainText = string.Concat(signParams.Values);

        //调用工具类统一入口，支持多算法
        return MqttSignCrypto.ComputeBySignMethod(param.SignMethod, plainText, deviceSecret);
    }

    #endregion
}
