using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Signs;

/// <summary>
/// MQTT ClientId 解析相关的正则表达式常量
/// MQTT ClientId 格式参见<see cref="MqttSign.Calculate(string, string, string, string, string?)"/>
/// </summary>
public static class MqttClientIdPatternConsts
{
    /// <summary>
    /// 提取 ProductKey：匹配开头的字母数字组合（如 "myProduct" from "myProduct.myDevice|..."）
    /// </summary>
    public const string ProductKeyPattern = @"^([a-zA-Z0-9]+)\.";

    /// <summary>
    /// 提取 DeviceName：匹配第一个点和竖线间的部分（如 "myDevice" from "myProduct.myDevice|"）
    /// </summary>
    public const string DeviceNamePattern = @"\.(?<deviceName>[a-zA-Z0-9-]+)\|";

    /// <summary>
    /// 提取 SignMethod：匹配 signmethod= 后的非逗号内容（如 "HMACSHA256" from "signmethod=HMACSHA256,"）
    /// </summary>
    public const string SignMethodPattern = @"signmethod=(?<signMethod>[^,]+)";

    /// <summary>
    /// 提取 Timestamp：匹配 timestamp= 后的非空格/逗号内容（如 "1715447098" from "timestamp=1715447098,"）
    /// </summary>
    public const string TimestampPattern = @"timestamp=(?<timestamp>[^\s,]+)";
}
