using System;
using System.Collections.Generic;

namespace Artizan.IoT.Mqtts.Signs;

/// <summary>
/// 一型一密签名与认证工具（独立实现，严格遵循阿里云规范）
/// 支持两种模式：预注册/免预注册
/// 核心特性：
/// 1. ClientId格式：DeviceName|参数|（无ProductKey前缀）
/// 2. 安全模式：预注册固定=2，免预注册固定=-2（不可修改）
/// 3. 签名原文：clientId + deviceName + productKey + random + timestamp（ASCII升序拼接）
/// </summary>
public class OneProductOneSecretMqttSign
{
    private readonly MqttAuthType _authType;
    private readonly int _fixedSecureMode;

    /// <summary>
    /// 构造函数（绑定认证模式，自动设置固定安全模式）
    /// </summary>
    /// <param name="authType">一型一密认证模式</param>
    public OneProductOneSecretMqttSign(MqttAuthType authType)
    {
        if (!authType.IsOneProductOnSecretAuth())
        {
            throw new ArgumentException($"不支持的认证类型：{authType}，仅支持一型一密相关类型");
        }

        _authType = authType;
        _fixedSecureMode = authType.GetFixedSecureMode(); // 强制固定安全模式
    }

    /// <summary>
    /// 生成MQTT连接参数（设备端使用）
    /// </summary>
    /// <param name="productKey">产品Key（设备预烧录）</param>
    /// <param name="deviceName">设备名称（预注册=预烧录，免预注册=设备生成）</param>
    /// <param name="productSecret">产品密钥（设备预烧录）</param>
    /// <param name="random">随机数（设备生成，32位以内）</param>
    /// <param name="signMethod">签名算法</param>
    /// <param name="secureMode">兼容参数（实际使用固定值，传入无效）</param>
    /// <param name="instanceId">企业版实例ID（可选）</param>
    /// <returns>ClientId/UserName/Password/认证参数</returns>
    public (string MqttClientId, string MqttUserName, string MqttPassword, MqttAuthParams Params) GenerateConnectParams(
        string productKey,
        string deviceName,
        string productSecret,
        string random,
        MqttSignMethod signMethod = MqttSignMethod.HmacSha256,
        int? secureMode = null, // 兼容参数，实际忽略
        string? timestamp = null,
        string? instanceId = null)
    {
        // 1. 基础参数合法性校验
        ValidateBaseParams(productKey, deviceName, productSecret, random);

        // 2. 构建认证参数（安全模式强制使用固定值，忽略传入值）
        var authParams = BuildAuthParams(productKey, deviceName, random, signMethod, timestamp, instanceId);

        // 3. 生成各连接参数
        var clientId = BuildMqttClientId(authParams);
        var userName = BuildMqttUserName(productKey, deviceName);
        var password = GenerateMqttPassword(authParams, productSecret);

        return (clientId, userName, password, authParams);
    }

    /// <summary>
    /// 验证MQTT签名（服务端使用）
    /// </summary>
    /// <param name="clientId">客户端提交的ClientId</param>
    /// <param name="userName">客户端提交的UserName</param>
    /// <param name="password">客户端提交的Password（签名）</param>
    /// <param name="productSecret">服务端存储的产品密钥</param>
    /// <returns>认证结果</returns>
    public MqttAuthResult ValidateSign(string clientId, string userName, string password, string productSecret, MqttAuthParams? authParams = null)
    {
        try
        {
            // 1. 解析ClientId、UserName获取基础参数
            authParams ??= MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);
            if (authParams == null)
            {
                return CreateFailResult(2, "ClientId格式非法（需符合：DeviceName|authType=xxx,...|）");
            }

            // 2. 验证认证类型匹配
            if (authParams.AuthType != _authType)
            {
                return CreateFailResult(2, $"认证类型不匹配，预期：{_authType}");
            }

            // 3. 强制验证安全模式（必须匹配固定值）
            if (authParams.SecureMode != _fixedSecureMode)
            {
                return CreateFailResult(2, $"安全模式不匹配，{_authType}强制要求：{_fixedSecureMode}");
            }

            // 5. 核心参数完整性校验
            var validateMsg = MqttSignUtils.ValidateCoreParams(authParams);
            if (!string.IsNullOrEmpty(validateMsg))
            {
                return CreateFailResult(2, validateMsg);
            }

            // 6. ClientId格式校验（不能包含ProductKey前缀）
            //if (clientId.Contains('.') && clientId.StartsWith($"{authParams.ProductKey}."))
            //{
            //    return CreateFailResult(2, "ClientId前缀格式非法（一型一密不允许包含ProductKey前缀）");
            //}

            // 7. 重新计算签名并比对
            var expectedPassword = GenerateMqttPassword(authParams, productSecret);
            if (expectedPassword != password)
            {
                return CreateFailResult(4, "签名验证失败（Password不匹配）");
            }

            // 8. 认证通过（免预注册模式需业务层额外校验DeviceName唯一性）
            return new MqttAuthResult
            {
                IsSuccess = true,
                Code = 0,
                Message = "认证通过",
                Params = authParams
            };
        }
        catch (Exception ex)
        {
            return CreateFailResult(3, $"认证系统异常：{ex.Message}");
        }
    }

    #region 私有方法
    /// <summary>
    /// 验证基础参数合法性
    /// </summary>
    private void ValidateBaseParams(string productKey, string deviceName, string productSecret, string random)
    {
        if (!MqttSignUtils.IsValidProductKey(productKey))
        {
            throw new ArgumentException("ProductKey非法：长度4-30字符，支持字母、数字、_、-、@、()");
        }

        if (!MqttSignUtils.IsValidDeviceName(deviceName))
        {
            throw new ArgumentException("DeviceName非法：长度4-32字符，支持字母、数字、_、-、@、.、:");
        }

        if (string.IsNullOrWhiteSpace(productSecret))
        {
            throw new ArgumentNullException(nameof(productSecret), "ProductSecret不能为空");
        }

        if (string.IsNullOrWhiteSpace(random) || random.Length > 32)
        {
            throw new ArgumentException("随机数非法：不能为空且长度不超过32字符", nameof(random));
        }
    }

    /// <summary>
    /// 构建认证参数对象（安全模式固定）
    /// </summary>
    private MqttAuthParams BuildAuthParams(string productKey, string deviceName, string random, MqttSignMethod signMethod, string? timestamp, string? instanceId)
    {
        return new MqttAuthParams
        {
            AuthType = _authType,
            SecureMode = _fixedSecureMode, // 强制固定安全模式
            SignMethod = signMethod,
            ProductKey = productKey,
            DeviceName = deviceName,
            Random = random,
            Timestamp = timestamp,
            InstanceId = instanceId
        };
    }

    /// <summary>
    /// 构建ClientId（一型一密格式）
    /// </summary>
    private string BuildMqttClientId(MqttAuthParams param)
    {
        var paramList = new List<string>
        {
            $"authType={(int)param.AuthType}",
            $"secureMode={param.SecureMode}",
            $"signMethod={param.SignMethod.ToString()}",
            $"random={param.Random}"        // 一型一密必须添加random
        };

        if (!string.IsNullOrWhiteSpace(param.Timestamp))
        {
            paramList.Add($"timestamp={param.Timestamp}");
        }

        if (!string.IsNullOrWhiteSpace(param.InstanceId))
        {
            paramList.Add($"instanceId={param.InstanceId}");
        }

        // 格式（无ProductKey前缀）：DeviceName|参数1,参数2,...|
        return $"{param.DeviceName}|{string.Join(",", paramList)}|";
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
    private string GenerateMqttPassword(MqttAuthParams param, string productSecret)
    {
        // 按ASCII升序排列参数（clientId < deviceName < productKey < random < timestamp）
        var signParams = new SortedDictionary<string, string>
        {
            // 注意：1.一型一密:clientId=DeviceName；2.此处的clientId不是 MqttClientId。
            { "clientId", param.DeviceName }, 
            { "deviceName", param.DeviceName },
            { "productKey", param.ProductKey },
            { "random", param.Random },
            { "timestamp", param.Timestamp??"" }
        };

        // 先保证排序，若空则再删除
        if (string.IsNullOrWhiteSpace(param.Timestamp))
        {
            signParams.Remove("timestamp");
        }

        // 拼接签名原文（注意：无分割符）
        var plainText = string.Concat(signParams.Values);

        // 计算并返回签名
        return MqttSignCrypto.ComputeBySignMethod(param.SignMethod, plainText, productSecret);
    }

    /// <summary>
    /// 创建认证失败结果
    /// </summary>
    private MqttAuthResult CreateFailResult(int code, string message)
    {
        return new MqttAuthResult
        {
            IsSuccess = false,
            Code = code,
            Message = message
        };
    }
    #endregion
}
