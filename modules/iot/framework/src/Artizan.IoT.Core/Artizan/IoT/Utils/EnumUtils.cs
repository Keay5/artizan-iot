using System;
using System.Collections.Generic;

namespace Artizan.IoT.Utils;

public class EnumUtils
{
    /// <summary>
    /// 解析枚举类型参数（严格校验，仅返回枚举中定义的有效值）
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="paramDict">参数字典</param>
    /// <param name="key">参数名</param>
    /// <param name="defaultValue">默认值（枚举中定义的有效值）</param>
    /// <returns>枚举有效值（非法值返回默认值）</returns>
    /// <example>
    /// 基于MqttAuthType枚举的使用示例：
    /// <code>
    /// // 定义参数字典
    /// var paramDict = new Dictionary&lt;string, string&gt;();
    /// 
    /// // 示例1：解析存在且有效的枚举名称字符串
    /// paramDict["AuthType"] = "OneDeviceOneSecret";
    /// MqttAuthType authType1 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneDeviceOneSecret（枚举值1）
    /// 
    /// // 示例2：解析存在且有效的枚举数值字符串
    /// paramDict["AuthType"] = "2";
    /// MqttAuthType authType2 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneProductOneSecretPreRegister（枚举值2）
    /// 
    /// // 示例3：解析不存在的参数，返回默认值
    /// paramDict.Clear();
    /// MqttAuthType authType3 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneDeviceOneSecret（默认值）
    /// 
    /// // 示例4：解析空白值参数，返回默认值
    /// paramDict["AuthType"] = "   ";
    /// MqttAuthType authType4 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneDeviceOneSecret（默认值）
    /// 
    /// // 示例5：解析未定义的枚举数值（如0），返回默认值
    /// paramDict["AuthType"] = "0";
    /// MqttAuthType authType5 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneDeviceOneSecret（默认值，因为0不是MqttAuthType定义的值）
    /// 
    /// // 示例6：解析无效字符串，返回默认值
    /// paramDict["AuthType"] = "InvalidType";
    /// MqttAuthType authType6 = ParseEnumParam(paramDict, "AuthType", MqttAuthType.OneDeviceOneSecret);
    /// // 返回：MqttAuthType.OneDeviceOneSecret（默认值）
    /// </code>
    /// </example>
    public static T ParseEnumParam<T>(IDictionary<string, string> paramDict, string key, T defaultValue)
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
}
