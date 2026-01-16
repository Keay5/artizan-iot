using Artizan.IoT.Devices;
using Artizan.IoT.Products;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// MQTT认证工具类（参数解析、格式验证等通用逻辑）
/// </summary>
public static class MqttSignUtils
{
    /// <summary>
    /// 解析 MqttClientId 和 MqttUserName 获取认证参数（无值时显式赋值为null）
    /// 格式要求：
    /// 1.ClientId=前缀|authType=xxx,secureMode=xxx,signMethod=xxx,...|
    /// 2.UserName=DeviceName&ProductKey
    /// </summary>
    /// <param name="mqttClientId">MQTT连接的ClientId</param>
    /// <param name="mqttUserName">MQTT连接的UserName</param>
    /// <returns>认证参数（无对应值的字段均为null）</returns>
    public static MqttAuthParams? ParseMqttClientIdAndUserName(string mqttClientId, string mqttUserName)
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

        // 5. 构建认证参数（无对应值的字段显式赋值为null）
        return new MqttAuthParams
        {
            // 从UserName提取的参数（无值时已为null）
            ProductKey = productKey,
            DeviceName = deviceName,

            // 从ClientId参数解析（无参数/解析失败时返回默认值，此处默认值符合业务预期）
            AuthType = ParseEnumParam<MqttAuthType>(paramDict, "authType", MqttAuthType.OneDeviceOneSecret),
            SecureMode = ParseIntParam(paramDict, "secureMode", MqttSecureModeConstants.Tcp),
            SignMethod = ParseEnumParam<MqttSignMethod>(paramDict, "signMethod", MqttSignMethod.HmacSha256),

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

    /// <summary>
    /// 验证ProductKey合法性
    /// </summary>
    public static bool IsValidProductKey(string productKey)
    {
        return !string.IsNullOrWhiteSpace(productKey) &&
               ProductConsts.ProductKeyRegex.IsMatch(productKey); ;
    }

    /// <summary>
    /// 验证DeviceName合法性
    /// </summary>
    public static bool IsValidDeviceName(string deviceName)
    {
        return !string.IsNullOrWhiteSpace(deviceName) &&
              DeviceConsts.DeviceNameRegex.IsMatch(deviceName);
    }

    /// <summary>
    /// 验证核心参数完整性（根据认证类型差异化校验）
    /// </summary>
    public static string? ValidateCoreParams(MqttAuthParams param)
    {
        if (string.IsNullOrWhiteSpace(param.ProductKey))
        {
            return "ProductKey不能为空";
        }

        if (string.IsNullOrWhiteSpace(param.DeviceName))
        {
            return "DeviceName不能为空";
        }

        // 一型一密必须包含随机数
        if (param.AuthType.IsOneProductOnSecretAuth() && string.IsNullOrWhiteSpace(param.Random))
        {
            return "一型一密认证必须包含随机数（random参数）";
        }

        return null;
    }

    #region 私有辅助方法

    /// <summary>
    /// 解析枚举类型参数（严格校验，仅返回枚举中定义的有效值）
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="paramDict">参数字典</param>
    /// <param name="key">参数名</param>
    /// <param name="defaultValue">默认值（枚举中定义的有效值）</param>
    /// <returns>枚举有效值（非法值返回默认值）</returns>
    private static T ParseEnumParam<T>(IDictionary<string, string> paramDict, string key, T defaultValue)
        where T : struct, Enum
    {
        // 1. 从字典获取参数值（无参数直接返回默认值）
        if (!paramDict.TryGetValue(key, out var valueStr) || string.IsNullOrWhiteSpace(valueStr))
        {
            return defaultValue;
        }

        // 2. 尝试解析枚举（支持字符串名称/数值字符串）
        if (Enum.TryParse<T>(valueStr, out T result))
        {
            // 3. 关键校验：确保解析结果是枚举中明确定义的值
            if (Enum.IsDefined(typeof(T), result))
            {
                return result; // 有效枚举值，直接返回
            }
        }

        // 4. 非法值（未定义的数值/无效字符串）返回默认值
        return defaultValue;
    }

    /// <summary>
    /// 解析整数类型参数（支持负数）
    /// </summary>
    private static int ParseIntParam(IDictionary<string, string> paramDict, string key, int defaultValue)
    {
        if (paramDict.TryGetValue(key, out var valueStr) && int.TryParse(valueStr, out int result))
        {
            return result;
        }
        return defaultValue;
    }
    #endregion
}
