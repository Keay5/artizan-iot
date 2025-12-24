using System;

namespace Artizan.IoT.Mqtts.Signs;

/// <summary>
/// MQTT认证工厂（统一入口，适配所有认证类型）
/// 职责：屏蔽底层实现差异，提供一致的调用体验
/// </summary>
public static class MqttSignAuthManager
{
    /// <summary>
    /// 根据认证类型创建对应的认证工具实例
    /// </summary>
    public static object CreateAuthTool(MqttAuthType authType)
    {
        return authType switch
        {
            MqttAuthType.OneDeviceOneSecret => new OneDeviceOneSecretMqttSign(),
            MqttAuthType.OneProductOneSecretPreRegister => new OneProductOneSecretMqttSign(authType),
            MqttAuthType.OneProductOneSecretNoPreRegister => new OneProductOneSecretMqttSign(authType),
            _ => throw new NotSupportedException($"不支持的认证类型：{authType}")
        };
    }

    /// <summary>
    /// 统一生成MQTT连接参数（设备端统一调用入口）
    /// </summary>
    public static (string MqttClientId, string MqttUserName, string MqttPassword, MqttAuthParams Params) GenerateConnectParams(
        MqttAuthType authType,
        string productKey = "",
        string deviceName = "",
        string secret = "", // 一机一密=DeviceSecret；一型一密=ProductSecret；Normal=无效
        string? random = null, // 一型一密必填
        MqttSignMethod signMethod = MqttSignMethod.HmacSha256,
        int? secureMode = null,
        string? timestamp = null,
        string? instanceId = null)
    {
        return authType switch
        {
            // 一机一密：需要ProductKey+DeviceName+DeviceSecret
            MqttAuthType.OneDeviceOneSecret =>
                ((OneDeviceOneSecretMqttSign)CreateAuthTool(authType))
                     .GenerateConnectParams(productKey, deviceName, secret, signMethod, secureMode, timestamp, random, instanceId),

            // 一型一密：需要ProductKey+DeviceName+ProductSecret+Random
            MqttAuthType.OneProductOneSecretPreRegister or MqttAuthType.OneProductOneSecretNoPreRegister =>
                ((OneProductOneSecretMqttSign)CreateAuthTool(authType))
                     .GenerateConnectParams(productKey, deviceName, secret, random ?? throw new ArgumentNullException(nameof(random)), signMethod, secureMode, timestamp, instanceId),

            _ => throw new NotSupportedException($"不支持的认证类型：{authType}")
        };
    }

    /// <summary>
    /// 统一验证MQTT签名（服务端统一调用入口，自动解析认证类型，无需手动传参）
    /// </summary>
    /// <param name="mqttClientId">MQTT客户端提交的ClientId</param>
    /// <param name="mqttUserName">MQTT客户端提交的UserName</param>
    /// <param name="mqttPassword">MQTT客户端提交的Password（签名值）</param>
    /// <param name="secret">
    /// 秘钥：
    ///     一机一密=DeviceSecret；
    ///     一型一密=ProductSecret
    /// </param>
    /// <returns>签名验证结果（包含解析状态和认证信息）</returns>
    public static MqttAuthResult ValidateSign(
        string mqttClientId,
        string mqttUserName,
        string mqttPassword,
        string secret)
    {
        try
        {
            // 步骤1：校验输入参数基础合法性
            var paramCheckResult = ValidateBaseParameters(mqttClientId, mqttUserName, mqttPassword, secret);
            if (!paramCheckResult.IsSuccess)
            {
                return paramCheckResult;
            }

            // 步骤2：解析ClientId和UserName，提取认证参数（含AuthType）
            var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(mqttClientId, mqttUserName);
            if (authParams == null)
            {
                return CreateFailResult(2, "ClientId格式非法，无法解析认证参数（需符合：前缀|authType=xxx,...|）");
            }

            // 步骤3：使用解析到的AuthType执行验证
            return ValidateSignByAuthType(authParams.AuthType, mqttClientId, mqttUserName, mqttPassword, secret, authParams);
        }
        catch (Exception ex)
        {
            return CreateFailResult(3, $"签名验证系统异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 重载：指定认证类型验证MQTT签名（适用于已知AuthType的场景）
    /// </summary>
    public static MqttAuthResult ValidateSign(
        MqttAuthType authType,
        string mqttClientId,
        string mqttUserName,
        string mqttPassword,
        string secret)
    {
        try
        {
            // 步骤1：校验输入参数基础合法性
            var paramCheckResult = ValidateBaseParameters(mqttClientId, mqttUserName, mqttPassword, secret);
            if (!paramCheckResult.IsSuccess)
            {
                return paramCheckResult;
            }

            // 步骤2：解析ClientId和UserName，提取认证参数
            var authParams = MqttSignUtils.ParseMqttClientIdAndUserName(mqttClientId, mqttUserName);
            if (authParams == null)
            {
                return CreateFailResult(2, "ClientId格式非法，无法解析认证参数（需符合：前缀|authType=xxx,...|）");
            }

            // 步骤3：强制使用传入的AuthType（覆盖解析结果）
            authParams.AuthType = authType;

            // 步骤4：使用指定的AuthType执行验证
            return ValidateSignByAuthType(authType, mqttClientId, mqttUserName, mqttPassword, secret, authParams);
        }
        catch (Exception ex)
        {
            return CreateFailResult(3, $"签名验证系统异常：{ex.Message}");
        }
    }


    #region 提取 ValidateSign 的公共逻辑方法
    /// <summary>
    /// 验证基础参数合法性（两个重载方法共用）
    /// </summary>
    private static MqttAuthResult ValidateBaseParameters(string clientId, string userName, string password, string secret)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CreateFailResult(2, "ClientId不能为空");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return CreateFailResult(2, "UserName不能为空");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return CreateFailResult(2, "Password（签名）不能为空");
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            return CreateFailResult(2, "验证秘钥（secret）不能为空");
        }

        return new MqttAuthResult { IsSuccess = true };
    }

    /// <summary>
    /// 根据指定的AuthType执行核心验证逻辑（两个重载方法共用）
    /// </summary>
    private static MqttAuthResult ValidateSignByAuthType(
        MqttAuthType authType,
        string mqttClientId,
        string mqttUserName,
        string mqttPassword,
        string secret,
        MqttAuthParams authParams)
    {
        // 校验核心参数完整性（ProductKey/DeviceName等）
        var coreParamError = MqttSignUtils.ValidateCoreParams(authParams);
        if (!string.IsNullOrEmpty(coreParamError))
        {
            return CreateFailResult(2, coreParamError);
        }

        // 创建对应认证工具
        object authTool;
        try
        {
            authTool = CreateAuthTool(authType);
        }
        catch (NotSupportedException ex)
        {
            return CreateFailResult(2, ex.Message);
        }

        // 执行对应类型的签名验证
        MqttAuthResult result = authType switch
        {
            MqttAuthType.OneDeviceOneSecret =>
                ((OneDeviceOneSecretMqttSign)authTool)
                    .ValidateSign(mqttClientId, mqttUserName, mqttPassword, secret, authParams),

            MqttAuthType.OneProductOneSecretPreRegister or MqttAuthType.OneProductOneSecretNoPreRegister =>
                ((OneProductOneSecretMqttSign)authTool)
                    .ValidateSign(mqttClientId, mqttUserName, mqttPassword, secret, authParams),

            _ => CreateFailResult(2, $"不支持的认证类型：{authType}")
        };

        // 补充解析后的参数到返回结果
        if (result.Params == null)
        {
            result.Params = authParams;
        }

        return result;
    }

    #endregion

    #region 私有辅助方法
    /// <summary>
    /// 创建认证失败结果
    /// </summary>
    private static MqttAuthResult CreateFailResult(int code, string message)
    {
        return new MqttAuthResult
        {
            IsSuccess = false,
            Code = code,
            Message = message,
            Params = null
        };
    }
    #endregion

}
