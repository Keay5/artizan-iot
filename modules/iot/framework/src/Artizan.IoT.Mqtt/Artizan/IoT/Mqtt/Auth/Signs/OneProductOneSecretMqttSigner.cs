using Artizan.IoT.Mqtt.Exceptions;
using Artizan.IoT.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT 一型一密签名器
/// 支持两种模式：预注册/免预注册
/// 核心特性：
/// 1. ClientId格式：DeviceName|参数|（无ProductKey前缀）
/// 2. 安全模式：预注册固定=2，免预注册固定=-2（不可修改）
/// 3. 签名原文：clientId + deviceName + productKey + random + timestamp（ASCII升序拼接）
/// </summary>
public class OneProductOneSecretMqttSigner : IMqttSigner, ITransientDependency
{
    public MqttAuthType AuthType { get; protected set; }

    /// <summary>
    /// 构造函数（绑定认证模式，自动设置固定安全模式）
    /// </summary>
    /// <param name="authType">一型一密认证模式</param>
    public OneProductOneSecretMqttSigner(MqttAuthType authType)
    {
        if (!authType.IsOneProductOnSecretAuth())
        {
            throw new ArgumentException($"不支持的认证类型：{authType}，仅支持一型一密相关类型");
        }

        AuthType = authType;
    }

    /// <inheritdoc/>
    public MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret)
    {
        MqttSignHelper.ValidateProductSecret(secret).CheckError<MqttResultException>();

        ValidateMqttSignParams(signParams).CheckError<MqttResultException>();

        // 生成各连接参数
        var clientId = BuildMqttClientId(signParams);
        var userName = BuildMqttUserName(signParams.ProductKey, signParams.DeviceName);
        var password = GenerateMqttPassword(signParams, secret);

        return new MqttConnectParams
        {
            ClientId = clientId,
            UserName = userName,
            Password = password
        };
    }

    /// <inheritdoc/>
    public MqttAuthResult VerifyMqttSign(MqttConnectParams connectParams, string secret)
    {
        var errorResults = new List<MqttAuthResult>();

        var parseResult = ParseMqttSignParams(connectParams);
        if (parseResult.Succeeded)
        {
            var signParams = parseResult.Data!;
            // 重新计算签名并比对
            var expectedPassword = GenerateMqttPassword(signParams, secret);
            if (expectedPassword != connectParams.Password)
            {
                errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.SignatureVerifyFailed, "签名验证失败（Password不匹配）"));
            }
        }
        else
        {
            errorResults.Add(parseResult);
        }

        return errorResults.Count > 0
            ? MqttAuthResult.Combine(errorResults.ToArray())
            : MqttAuthResult.Success(parseResult.Data);
    }

    /// <summary>
    /// 从MQTT连接参数中解析出MQTT签名参数
    /// </summary>
    /// <param name="connectParams">MQTT 连接参数</param>
    /// <returns></returns>
    private MqttAuthResult ParseMqttSignParams(MqttConnectParams connectParams)
    {
        //  从MQTT连接参数中解析出签名参数
        var signParams = MqttSignHelper.ParseMqttClientIdAndUserName(connectParams.ClientId, connectParams.UserName);

        // 验证解析结果,无法解析则格式非法
        if (signParams == null)
        {
            // 当(signParams == null) ，直接返回,因为后续验证均基于signParams 不为null
            return MqttAuthResult.Failed(IoTMqttErrorCodes.ClientIdFormatInvalid, "ClientId格式非法（需符合：ProductKey.DeviceName|authType=xxx,...|）");
        }

        var result = MqttAuthResult.Combine(
            ValidateMqttSignParams(signParams),
            ValidateMqttConnectParams(connectParams, signParams!.ProductKey)
        );

        return result.Errors.Any()
            ? result
            : MqttAuthResult.Success(signParams);
    }

    private MqttAuthResult ValidateMqttSignParams(MqttSignParams? signParams)
    {
        // 验证解析结果,无法解析则格式非法
        if (signParams == null)
        {
            return MqttAuthResult.Failed(
                IoTMqttErrorCodes.ClientIdFormatInvalid, 
                "ClientId格式非法（需符合：ProductKey.DeviceName|authType=xxx,...|）");
        }

        var errorResults = new List<MqttAuthResult>();

        // 校验 MQTT 签名通用参数
        var validateCommonParamsResult = MqttSignHelper.ValidateMqttSignCommonParams(signParams);
        if (!validateCommonParamsResult.Succeeded)
        {
            errorResults.Add(validateCommonParamsResult);
        }

        // 验证认证类型匹配
        if (signParams.AuthType != AuthType)
        {
            errorResults.Add(MqttAuthResult.Failed(
                IoTMqttErrorCodes.AuthTypeMismatch, 
                $"认证类型不匹配，预期：{MqttAuthType.OneDeviceOneSecret}"));
        }

        // 一型一密必须包含随机数
        if (signParams.AuthType.IsOneProductOnSecretAuth() && string.IsNullOrWhiteSpace(signParams.Random))
        {
            errorResults.Add(MqttAuthResult.Failed(
                IoTMqttErrorCodes.OneProductOneSecretAuthParamRandomCanNotBeNull, 
                "一型一密认证必须包含随机数（random参数）"));
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success(signParams);
    }

    private MqttAuthResult ValidateMqttConnectParams(MqttConnectParams connectParams, string productKey)
    {
        var errorResults = new List<MqttAuthResult>();

        // MQTT ClientId格式校验（必须包含ProductKey前缀）
        if (!connectParams.ClientId.StartsWith($"{productKey}."))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.ClientIdFormatInvalid, "ClientId前缀格式非法（需符合：ProductKey.DeviceName）"));
        }

        return errorResults.Count > 0
           ? MqttAuthResult.Combine(errorResults.ToArray())
           : MqttAuthResult.Success();
    }

    #region 构建MQTT连接参数

    /// <summary>
    /// 构建ClientId（一型一密格式）
    /// </summary>
    private string BuildMqttClientId(MqttSignParams signParams)
    {
        var paramList = new List<string>
        {
            $"authType={(int)signParams.AuthType}",
            // $"secureMode={signParams.SecureMode}", //降低复杂度：安全模式，不需要添加到ClientId中
            $"signMethod={signParams.SignMethod.ToString()}",
            $"random={signParams.Random}"        // 一型一密必须添加random
        };

        if (!string.IsNullOrWhiteSpace(signParams.Timestamp))
        {
            paramList.Add($"timestamp={signParams.Timestamp}");
        }

        if (!string.IsNullOrWhiteSpace(signParams.InstanceId))
        {
            paramList.Add($"instanceId={signParams.InstanceId}");
        }

        /* ----------------------------------------------------------------------------------
         * 降低复杂度，统一使用 MqttClientId 的格式为：ProductKey.DeviceName|参数1,参数2,...|
         */
        //// 格式（无ProductKey前缀）：DeviceName|参数1,参数2,...|
        //return $"{signParams.DeviceName}|{string.Join(",", paramList)}|";

        // 格式：ProductKey.DeviceName|参数1,参数2,...|
        return $"{signParams.ProductKey}.{signParams.DeviceName}|{string.Join(",", paramList)}|";
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
    /// 签名原文：按ASCII升序排列 clientId、deviceName、productKey、random、timestamp 并拼接
    /// </summary>
    private string GenerateMqttPassword(MqttSignParams signParams, string productSecret)
    {
        // 按ASCII升序排列参数（clientId < deviceName < productKey < random < timestamp）
        var signParamsSorted = new SortedDictionary<string, string>
        {
            // 注意：1.一型一密:clientId=DeviceName；2.此处的clientId不是 MqttClientId。
            { "clientId", signParams.DeviceName },
            { "deviceName", signParams.DeviceName },
            { "productKey", signParams.ProductKey },
            { "random", signParams.Random??"" },
            { "timestamp", signParams.Timestamp??"" }
        };

        #region Vaulue空，可保留Key，降低算法的设计复杂度
        //// 先保证排序，若空则再删除
        //if (string.IsNullOrWhiteSpace(signParams.Timestamp))
        //{
        //    signParamsSorted.Remove("timestamp");
        //} 
        #endregion

        // 拼接签名原文（注意：无分割符）
        var plainText = string.Concat(signParamsSorted.Values);

        // 计算并返回签名
        return MqttSignCrypto.ComputeBySignMethod(signParams.SignMethod, plainText, productSecret);
    }

    #endregion
}
