using Artizan.IoT.Devices;
using Artizan.IoT.Products;
using Artizan.IoT.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT签名工具类（参数解析、格式验证等通用逻辑）
/// </summary>
public static class MqttSignHelper
{
    /// <summary>
    /// 解析 MqttClientId 和 MqttUserName 获取MQTT 签名参数（无值时显式赋值为null）
    /// 格式要求：
    /// 1.ClientId=前缀|authType=xxx,secureMode=xxx,signMethod=xxx,...|
    /// 2.UserName=DeviceName&ProductKey
    /// </summary>
    /// <param name="mqttClientId">MQTT连接的ClientId</param>
    /// <param name="mqttUserName">MQTT连接的UserName</param>
    /// <returns>签名参数（无对应值的字段均为null）</returns>
    public static MqttSignParams? ParseMqttClientIdAndUserName(string mqttClientId, string mqttUserName)
    {
        // 1. 校验ClientId基础格式（非空且包含分隔符|）
        if (string.IsNullOrWhiteSpace(mqttClientId) || !mqttClientId.Contains('|'))
        {
            return null;
        }

        // 2. 拆分ClientId（前缀|参数部分|，过滤空项）
        var clientIdParts = mqttClientId.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (clientIdParts.Length < 2)
        {
            return null;
        }

        // 3. 解析参数部分为字典（key=value格式，忽略非法项）
        // 示例：clientIdParts[1] = "authType=3, secureMode=-2, random=abc=123=def, timestamp= 1699999999999 , emptyKey=, , invalidParam"
        var paramDict = clientIdParts[1].Split(',') // 步骤1：按逗号分割参数部分 → 得到数组：
                                                    // [
                                                    //   "authType=3", 
                                                    //   " secureMode=-2", 
                                                    //   " random=abc=123=def", 
                                                    //   " timestamp= 1699999999999 ", 
                                                    //   " emptyKey=", 
                                                    //   " ", 
                                                    //   " invalidParam"
                                                    // ]
            .Where(item => !string.IsNullOrWhiteSpace(item) && item.Contains('=')) // 步骤2：过滤非法项（非空且包含=）→ 保留：
                                                                                   // [
                                                                                   //   "authType=3", 
                                                                                   //   " secureMode=-2", 
                                                                                   //   " random=abc=123=def", 
                                                                                   //   " timestamp= 1699999999999 ", 
                                                                                   //   " emptyKey="
                                                                                   // ]
            .ToDictionary(
                keySelector: item =>
                {
                    // 步骤3.1：解析字典的Key（取=左侧内容，Trim空格）
                    var keyParts = item.Split('=')[0].Trim();
                    // 示例：item=" secureMode=-2" → Split('=')[0] = " secureMode" → Trim() → "secureMode"
                    return keyParts;
                },
                elementSelector: item =>
                {
                    // 步骤3.2：解析字典的Value（取=右侧内容，仅按第一个=拆分，避免值中包含=）
                    var valueParts = item.Split('=', 2); // 最多拆分2部分，示例：
                                                         // item=" random=abc=123=def" → Split('=',2) → [" random", "abc=123=def"]
                                                         // item=" emptyKey=" → Split('=',2) → [" emptyKey", ""]

                    // 有有效值则Trim空格，无有效值（空字符串）返回null
                    return valueParts.Length == 2 ? valueParts[1].Trim() : null;
                    // 示例1：valueParts[1] = "abc=123=def" → Trim() → "abc=123=def"
                    // 示例2：valueParts[1] = " 1699999999999 " → Trim() → "1699999999999"
                    // 示例3：valueParts[1] = "" → 返回null
                },
                StringComparer.OrdinalIgnoreCase // 步骤3.3：字典Key忽略大小写（如AuthType、authtype视为同一Key）
            );
        // 最终解析结果（示例）：
        // paramDict = {
        //   "authType": "3",
        //   "secureMode": "-2",
        //   "random": "abc=123=def",
        //   "timestamp": "1699999999999",
        //   "emptyKey": null
        // }


        // 4. 从UserName提取ProductKey和DeviceName（无值时返回null）
        var (productKey, deviceName) = ExtractProductAndDevice(mqttUserName);

        // 5. 构建MQTT 签名参数（无对应值的字段显式赋值为null）
        return new MqttSignParams
        {
            // 从UserName提取的参数（无值时已为null）
            ProductKey = productKey,
            DeviceName = deviceName,

            // 从ClientId参数解析（无参数/解析失败时返回默认值，此处默认值符合业务预期）
            AuthType = EnumUtils.ParseEnumParam<MqttAuthType>(paramDict, "authType", MqttAuthType.OneDeviceOneSecret),
            //SecureMode = ParseIntParam(paramDict, "secureMode", MqttSecureModeConstants.Tcp),
            SignMethod = EnumUtils.ParseEnumParam<MqttSignMethod>(paramDict, "signMethod", MqttSignMethod.HmacSha256),

            // 从ClientId参数提取（无参数时显式赋值为null）
            Timestamp = paramDict.TryGetValue("timestamp", out var timestamp) && !string.IsNullOrWhiteSpace(timestamp)
                ? timestamp
                : null,
            Random = paramDict.TryGetValue("random", out var random) && !string.IsNullOrWhiteSpace(random)
                ? random
                : null,
            InstanceId = paramDict.TryGetValue("instanceId", out var instanceId) && !string.IsNullOrWhiteSpace(instanceId)
                ? instanceId
                : null
        };
    }

    /// <summary>
    /// 从UserName提取ProductKey和DeviceName（格式：DeviceName&ProductKey）
    /// </summary>
    public static (string? ProductKey, string? DeviceName) ExtractProductAndDevice(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName) || !userName.Contains('&'))
        {
            return (null, null);
        }

        var parts = userName.Split('&');
        var deviceName = parts[0];
        var productKey = parts[1];

        productKey = !string.IsNullOrWhiteSpace(productKey) ? productKey : null;
        deviceName = !string.IsNullOrWhiteSpace(deviceName) ? deviceName : null;

        return (productKey, deviceName);
    }

    #region 校验参数合法性

    /// <summary>
    /// 校验 ProductKey
    /// </summary>
    public static MqttAuthResult ValidateProductKey(string productKey)
    {
        var errorResults = new List<MqttAuthResult>();

        if (string.IsNullOrWhiteSpace(productKey))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.ProductKeyCanNotBeNull));
        }

        if (!ProductConsts.ProductKeyRegex.IsMatch(productKey))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.ProductKeyInvalid));
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success();
    }

    /// <summary>
    /// 校验 ProductSecret
    /// </summary>
    public static MqttAuthResult ValidateProductSecret(string productSecret)
    {
        var errorResults = new List<MqttAuthResult>();

        if (string.IsNullOrWhiteSpace(productSecret))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.ProductSecrtCanNotBeNull));
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success();
    }

    /// <summary>
    /// 校验 DeviceName
    /// </summary>
    public static MqttAuthResult ValidateDeviceName(string deviceName)
    {
        var errorResults = new List<MqttAuthResult>();

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.DeviceNameInvalid));
        }
        if (!DeviceConsts.DeviceNameRegex.IsMatch(deviceName))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.DeviceNameCanNotBeNull));
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success();
    }

    /// <summary>
    /// 校验 DeviceSecret
    /// </summary>
    public static MqttAuthResult ValidateDeviceSecret(string deviceSecret)
    {
        var errorResults = new List<MqttAuthResult>();

        if (string.IsNullOrWhiteSpace(deviceSecret))
        {
            errorResults.Add(MqttAuthResult.Failed(IoTMqttErrorCodes.DeviceSecrtCanNotBeNull));
        }
        return errorResults.Count > 0
            ? MqttAuthResult.Combine(errorResults.ToArray())
            : MqttAuthResult.Success();
    }

    /// <summary>
    /// 校验 Mqtt 签名通用参数，
    /// 注意：这里只做通用（共性）参数校验。
    //        其它差异化参数的校验，留给具体的业务类（比如：MQTT签名器实现类）处理。
    /// </summary>
    public static MqttAuthResult ValidateMqttSignCommonParams(MqttSignParams signParams)
    {
        var errorResults = new List<MqttAuthResult>();

        var result = ValidateProductKey(signParams.ProductKey);
        if (!result.Succeeded)
        {
            errorResults.Add(result);
        }

        result = ValidateDeviceName(signParams.DeviceName);
        if (!result.Succeeded)
        {
            errorResults.Add(result);
        }

        if (signParams.AuthType == null)
        {
            errorResults.Add(MqttAuthResult.Failed(
                 IoTMqttErrorCodes.ClientIdFormatInvalid,
                 $"认证方式不能为空"));
        }

        // 额外校验枚举值是否在定义的有效值范围内（防止枚举有未赋值的默认值）
        if (!Enum.IsDefined(typeof(MqttAuthType), signParams.AuthType))
        {
            errorResults.Add(MqttAuthResult.Failed(
                IoTMqttErrorCodes.AuthTypeInvalid,
                $"认证类型AuthType不在合法范围内，当前值：{signParams.AuthType}"));
        }

        if (signParams.SignMethod == null)
        {
            errorResults.Add(MqttAuthResult.Failed(
                IoTMqttErrorCodes.ClientIdFormatInvalid,
                $"签名算法不能为空"));
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success(signParams);
    }

    #endregion

}
