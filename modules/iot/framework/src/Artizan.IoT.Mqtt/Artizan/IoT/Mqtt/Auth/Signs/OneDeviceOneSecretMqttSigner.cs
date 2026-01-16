using Artizan.IoT.Mqtt.Exceptions;
using Artizan.IoT.Results;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT 一型一密签名器
/// 核心特性：
/// 1. MQTT ClientId格式：ProductKey.DeviceName|参数|
/// 2. 安全模式：支持2（TCP）/3（TLS）
/// 3. 签名原文：见<see cref="GenerateMqttPassword"/> 方法
/// </summary>
public class OneDeviceOneSecretMqttSigner : IMqttSigner, ITransientDependency
{
    /// <inheritdoc/>
    public MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret)
    {
        MqttSignHelper.ValidateDeviceSecret(secret).CheckError<MqttResultException>();
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
        var errorResults = new List<MqttAuthResult>();

        // 验证解析结果,无法解析则格式非法
        if (signParams == null)
        {
            errorResults.Add( MqttAuthResult.Failed(
                 IoTMqttErrorCodes.ClientIdFormatInvalid, 
                "ClientId格式非法（需符合：ProductKey.DeviceName|authType=xxx,...|）"));
        }

        // 验证认证类型匹配
        if (signParams.AuthType != MqttAuthType.OneDeviceOneSecret)
        {
            errorResults.Add( MqttAuthResult.Failed(
                 IoTMqttErrorCodes.AuthTypeMismatch, 
                $"认证类型不匹配，预期：{MqttAuthType.OneDeviceOneSecret}"));
        }

        // 验证 MQTT 签名通用参数合法性
        var validateCommonParamsResult = MqttSignHelper.ValidateMqttSignCommonParams(signParams);
        if (!validateCommonParamsResult.Succeeded)
        {
            errorResults.Add(validateCommonParamsResult);
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
    /// 构建ClientId（一机一密格式）
    /// 格式：ProductKey.DeviceName|参数1,参数2,...|
    /// </summary>
    private string BuildMqttClientId(MqttSignParams signParams)
    {
        var paramList = new List<string>
        {
            $"authType={(int)signParams.AuthType}",
            //$"secureMode={param.SecureMode}",  // //降低复杂度：安全模式，不需要添加到ClientId中
            $"signMethod={signParams.SignMethod.ToString()}"
        };

        if (!string.IsNullOrWhiteSpace(signParams.Random))
        {
            paramList.Add($"random={signParams.Random}");
        }

        if (!string.IsNullOrWhiteSpace(signParams.Timestamp))
        {
            paramList.Add($"timestamp={signParams.Timestamp}");
        }

        if (!string.IsNullOrWhiteSpace(signParams.InstanceId))
        {
            paramList.Add($"instanceId={signParams.InstanceId}");
        }

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
    /// 生成签名（MQTT Password）
    /// 签名原文：按ASCII升序排列 clientId、deviceName、productKey、timestamp 并拼接
    /// </summary>
    private string GenerateMqttPassword(MqttSignParams signParams, string deviceSecret)
    {
        // 按ASCII升序排列参数（clientId < deviceName < productKey < random <timestamp）
        var signParamsSorted = new SortedDictionary<string, string>
        {
            // 注意：1.一机一密:clientId=ProductKey.DeviceName；2.此处的clientId不是 MqttClientId。
            { "clientId", $"{signParams.ProductKey}.{signParams.DeviceName}" },
            { "deviceName", signParams.DeviceName },
            { "productKey", signParams.ProductKey },
            { "random", signParams.Random??"" },
            { "timestamp", signParams.Timestamp??"" }
        };

        #region Vaulue空，可保留Key，降低算法的设计复杂度
        //// 先保证排序，若空则再删除
        //if (string.IsNullOrWhiteSpace(authParams.Random))
        //{
        //    signParams.Remove("random");
        //}

        //// 先保证排序，若空则再删除
        //if (string.IsNullOrWhiteSpace(authParams.Timestamp))
        //{
        //    signParams.Remove("timestamp");
        //} 
        #endregion

        // 拼接签名原文（注意：无分割符）
        var plainText = string.Concat(signParamsSorted.Values);

        //调用工具类统一入口，支持多算法
        return MqttSignCrypto.ComputeBySignMethod(signParams.SignMethod, plainText, deviceSecret);
    }

    #endregion

}
