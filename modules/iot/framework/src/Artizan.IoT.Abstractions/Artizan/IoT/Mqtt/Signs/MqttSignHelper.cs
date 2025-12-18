using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Artizan.IoT.Errors;
using Artizan.IoT.Localization;
using Artizan.IoT.Results;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Signs;

/// <summary>
/// MQTT签名工具类
/// </summary>
public static class MqttSignHelper
{
    /// <summary>
    /// 目前的方案是：
    /// 在Module初始化时赋值，参见 <see cref="IoTAbstractionsModule.OnApplicationInitialization(Volo.Abp.ApplicationInitializationContext)"/>
    /// TODO: 是否还有更好的方案？
    /// </summary>
    public static IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;
    private static IStringLocalizerFactory StringLocalizerFactory => LazyServiceProvider.LazyGetRequiredService<IStringLocalizerFactory>();

    /// <summary>
    /// 验证 MQTT 签名
    /// </summary>
    /// <param name="mqttClientId">MQTT Client ID</param>
    /// <param name="mqttClientPassword">MQTT Client Password</param>
    /// <param name="deviceSecret">设备秘钥</param>
    /// <returns></returns>
    public static IoTResult VerifyMqttSign(string mqttClientId, string mqttClientPassword, string deviceSecret)
    {
        Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));
        Check.NotNullOrEmpty(mqttClientPassword, nameof(mqttClientPassword));
        Check.NotNullOrEmpty(deviceSecret, nameof(deviceSecret));

        var (productKey, deviceName, signMethod, timestamp) = ParseMqttClientId(mqttClientId); // 可用_ 忽略不需要的字段

        return VerifyMqttSign(
            productKey,
            deviceName,
            deviceSecret,
            signMethod,
            mqttClientPassword,
            timestamp);
    }

    /// <summary>
    /// 验证 MQTT 签名
    /// </summary>
    /// <param name="productKey"></param>
    /// <param name="deviceName"></param>
    /// <param name="deviceSecret"></param>
    /// <param name="signMethod"></param>
    /// <param name="mqttClientPassword"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static IoTResult VerifyMqttSign( 
        string productKey,
        string deviceName,
        string deviceSecret,
        string signMethod,
        string mqttClientPassword,
        string? timestamp = null)
    {
        Check.NotNull(productKey, nameof(productKey));
        Check.NotNull(deviceName, nameof(deviceName));
        Check.NotNull(deviceSecret, nameof(deviceSecret));
        Check.NotNull(signMethod, nameof(signMethod));
        Check.NotNull(mqttClientPassword, nameof(mqttClientPassword));

        var mqttSign = new MqttSign();
        List<IoTError>? errors = null;

        mqttSign.Calculate(productKey, deviceName, deviceSecret, signMethod, timestamp);
        if (mqttClientPassword != mqttSign.Password)
        {
            var stringLocalizer = StringLocalizerFactory.Create<IoTResource>();
 
            errors ??= [];
            errors.Add(new IoTError()
            {
                Code = IoTErrorCodes.Mqtt.AuthenticationFailed,

                //Description = $"Device '{deviceName}' authentication failed."
                // TODO: 检查这种静态类本地化方案是成功？
                Description = stringLocalizer[
                    IoTErrorCodes.Mqtt.DeviceAuthenticationFailed,
                    deviceName
                ]
            });
        }

        return errors?.Count > 0 ? IoTResult.Failed(errors) : IoTResult.Success;
    }

    public static (string ProductKey, string DeviceName, string SignMethod, string? Timestamp)
        ParseMqttClientId(string mqttClientId)
    {
        var productKey = GetProductKey(mqttClientId);
        var deviceName = GetDeviceName(mqttClientId);
        var signMethod = GetSignMethod(mqttClientId);
        var timestamp = TryGetTimestampOrNull(mqttClientId);

        return (productKey, deviceName, signMethod, timestamp);
    }

    /// <summary>
    /// 从 MQTT ClientId 字符串中获取 ProductKey
    /// MQTT ClientId格式参见方法
    /// </summary>
    /// <param name="mqttClientId">Mqtt ClientId，参见<see cref="MqttSign.ClientId"/></param>
    /// <returns></returns>
    public static string GetProductKey(string mqttClientId)
    {
        Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

        // 引用常量类中的正则
        var match = Regex.Match(mqttClientId, MqttClientIdPatternConsts.ProductKeyPattern);

        if (!match.Success)
            throw new AbpException($"Invalid MQTT ClientId format: missing ProductKey prefix. MQTT ClientId: {mqttClientId}");

        var productKey = match.Groups[1].Value;
        if (string.IsNullOrEmpty(productKey))
            throw new AbpException($"Parsed ProductKey is empty. MQTT ClientId: {mqttClientId}");

        return productKey;
    }
    #region 暂时废弃
    //public static string GetProductKey(string mqttClientId)
    //{
    //    /*
    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));
    //    string productKey;

    //    try
    //    {
    //        //TODO: 提取 productKey 通配符
    //        // 定义正则：匹配开头的 ProductKey（第一个点前的字母数字组合）
    //        var pattern = @"^([a-zA-Z0-9]+)\.";
    //        var match = Regex.Match(mqttClientId, pattern);

    //        if (match.Success)
    //        {
    //            var matchProductKey = match.Groups[1].Value;
    //            if (!string.IsNullOrEmpty(matchProductKey))
    //            {
    //                productKey = matchProductKey;
    //            }
    //            else
    //            {
    //                throw new AbpException("Failed to parse ProductKey from Mqtt ClientId");
    //            }
    //        }
    //        else
    //        {
    //            throw new AbpException("Failed to parse ProductKey from Mqtt ClientId");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new AbpException("Failed to parse ProductKey from Mqtt ClientId", ex);
    //    }

    //    return productKey;
    //    */

    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    /*
    //    // 解析 ProductKey（第一个点前的部分，如 "myProduct" from "myProduct.myDevice|..."）
    //    var productKeyMatch = Regex.Match(mqttClientId, @"^([a-zA-Z0-9]+)\.");
    //    if (!productKeyMatch.Success || string.IsNullOrEmpty(productKeyMatch.Groups[1].Value))
    //    {
    //        throw new AbpException("Failed to parse ProductKey from Mqtt ClientId");
    //    }
    //    var productKey = productKeyMatch.Groups[1].Value;

    //    return productKey;
    //    */

    //    // 1. 校验输入非空（ABP 工具方法，编译时确保输入合法）
    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    // 2. 定义正则：匹配开头的 ProductKey（第一个点前的字母数字组合）
    //    var pattern = @"^([a-zA-Z0-9]+)\.";  // TODO: 提取 productKey 通配符，以保证全局统一
    //    var match = Regex.Match(mqttClientId, pattern);

    //    // 3. 运行时校验：匹配成功 + 提取值非空（避免返回 null）
    //    if (!match.Success)
    //    {
    //        throw new AbpException($"Invalid ClientId format: missing ProductKey prefix. ClientId: {mqttClientId}");
    //    }

    //    var productKey = match.Groups[1].Value;
    //    if (string.IsNullOrEmpty(productKey))
    //    {
    //        throw new AbpException($"Parsed ProductKey is empty. ClientId: {mqttClientId}");
    //    }

    //    // 4. 返回非空字符串（编译器确认此处不会返回 null）
    //    return productKey;
    //} 
    #endregion

    /// <summary>
    /// 从 MQTT ClientId 字符串中获取 DeviceName
    /// </summary>
    /// <param name="mqttClientId">Mqtt ClientId，参见<see cref="MqttSign.ClientId"/></param>
    /// <returns></returns>
    public static string GetDeviceName(string mqttClientId)
    {
        Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

        // 引用常量类中的正则
        var match = Regex.Match(mqttClientId, MqttClientIdPatternConsts.DeviceNamePattern);

        if (!match.Success)
        {
            throw new AbpException($"Invalid MQTT ClientId format: missing DeviceName part. MQTT ClientId: {mqttClientId}");
        }

        var deviceName = match.Groups["deviceName"].Value;
        if (string.IsNullOrEmpty(deviceName))
        {
            throw new AbpException($"Parsed DeviceName is empty. MQTT ClientId: {mqttClientId}");
        }

        return deviceName;
    }

    #region 暂时废弃
    //public static string GetDeviceName(string mqttClientId)
    //{
    //    /*
    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    string deviceName;

    //    try
    //    {
    //        // 匹配 deviceName 的正则表达式模式
    //        var pattern = @"\.([a-zA-Z0-9-]+)\|";  //TODO: 提取 deviceName 通配符
    //        var match = Regex.Match(mqttClientId, pattern);

    //        if (match.Success)
    //        {
    //            var matchDeviceName = match.Groups[1].Value;
    //            if (!string.IsNullOrEmpty(matchDeviceName))
    //            {
    //                deviceName = matchDeviceName;
    //            }
    //            else
    //            {
    //                throw new AbpException("Failed to parse DeviceName from Mqtt ClientId");
    //            }
    //        }
    //        else
    //        {
    //            throw new AbpException("Failed to parse DeviceName from Mqtt ClientId");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new AbpException("Failed to parse DeviceName from Mqtt ClientId", ex);
    //    }

    //    return deviceName;
    //    */

    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    // 解析 DeviceName（第一个点和竖线间的部分，如 "myDevice" from ".myDevice|"）
    //    var deviceNameMatch = Regex.Match(mqttClientId, @"\.(?<deviceName>[a-zA-Z0-9-]+)\|");
    //    if (!deviceNameMatch.Success || string.IsNullOrEmpty(deviceNameMatch.Groups["deviceName"].Value))
    //    {
    //        throw new AbpException("Failed to parse DeviceName from Mqtt ClientId");
    //    }
    //    var deviceName = deviceNameMatch.Groups["deviceName"].Value;

    //    return deviceName;
    //}
    #endregion

    /// <summary>
    /// 从 MQTT ClientId 字符串中获取签名算法(SignMethod)
    /// </summary>
    /// <param name="mqttClientId">Mqtt ClientId，参见<see cref="MqttSign.ClientId"/></param>
    /// <returns></returns>
    public static string GetSignMethod(string mqttClientId)
    {
        Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

        var match = Regex.Match(mqttClientId, MqttClientIdPatternConsts.SignMethodPattern, RegexOptions.IgnoreCase);

        if (!match.Success || string.IsNullOrEmpty(match.Groups["signMethod"].Value))
        {
            throw new AbpException($"Failed to parse SignMethod from MQTT ClientId: {mqttClientId}");
        }

        return match.Groups["signMethod"].Value;
    }

    #region 暂时废弃
    //public static string GetSignMethod(string mqttClientId)
    //{
    //    /*
    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    string signMethod;

    //    // 正则表达式：匹配 signmethod= 后的非逗号内容（不区分大小写）
    //    Regex regex = new Regex(@"signmethod=([^,]+)", RegexOptions.IgnoreCase);
    //    Match match = regex.Match(mqttClientId); // 搜索第一个匹配项

    //    try
    //    {
    //        if (match.Success)
    //        {
    //            var matchSignMethod = match.Groups[1].Value;
    //            if (!string.IsNullOrEmpty(matchSignMethod))
    //            {
    //                signMethod = matchSignMethod;
    //            }
    //            else
    //            {
    //                throw new AbpException("Failed to parse SignMethod from Mqtt ClientId");
    //            }
    //        }
    //        else
    //        {
    //            throw new AbpException("Failed to parse SignMethod from Mqtt ClientId");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new AbpException("Failed to parse SignMethod from Mqtt ClientId", ex);
    //    }

    //    return signMethod;
    //    */

    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    // 解析 SignMethod（signmethod=后的非逗号内容，必须存在）
    //    var signMethodMatch = Regex.Match(mqttClientId, @"signmethod=(?<signMethod>[^,]+)", RegexOptions.IgnoreCase);
    //    if (!signMethodMatch.Success || string.IsNullOrEmpty(signMethodMatch.Groups["signMethod"].Value))
    //    {
    //        throw new AbpException("Failed to parse SignMethod from Mqtt ClientId");
    //    }
    //    var signMethod = signMethodMatch.Groups["signMethod"].Value;

    //    return signMethod;
    //} 
    #endregion

    #region 暂时废弃
    //public static string? TryGetTimestampOrNull(string mqttClientId)
    //{
    //    Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));
    //    // 解析 Timestamp（timestamp=后的非空格/逗号内容，可选）
    //    string? timestamp = null;
    //    var timestampMatch = Regex.Match(mqttClientId, @"timestamp=(?<timestamp>[^\s,]+)");
    //    if (timestampMatch.Success && !string.IsNullOrEmpty(timestampMatch.Groups["timestamp"].Value))
    //    {
    //        timestamp = timestampMatch.Groups["timestamp"].Value;
    //    }

    //    return timestamp;

    //    //Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

    //    //string? timestamp;

    //    //try
    //    //{
    //    //    /*--------------------------------------------------------------------------------------------------
    //    //       timestamp=([^\s,]+):
    //    //       用于匹配任意非空白字符和逗号之外的字符，并且至少匹配一次。这个捕获组用于提取 timestamp 的值,
    //    //       如果应用于一个字符串，例如 "timestamp=12345678abcde, other_info"，那么匹配结果将是 "12345678abcde"。
    //    //     *------------------------------------------------------------------------------------------------*/
    //    //    var pattern = @"timestamp=([^\s,]+)";
    //    //    var match = Regex.Match(mqttClientId, pattern);

    //    //    if (match.Success)
    //    //    {
    //    //        var matchTimestamp = match.Groups[1].Value;
    //    //        if (!string.IsNullOrEmpty(matchTimestamp))
    //    //        {
    //    //            timestamp = matchTimestamp;
    //    //        }
    //    //        else
    //    //        {
    //    //            timestamp = null;
    //    //        }
    //    //    }
    //    //    else
    //    //    {
    //    //        timestamp = null;
    //    //    }
    //    //}
    //    //catch (Exception ex)
    //    //{
    //    //    throw new AbpException("Failed to parse Timestamp from Mqtt ClientId", ex);
    //    //}

    //    //return timestamp;
    //}
    #endregion
    public static string? TryGetTimestampOrNull(string mqttClientId)
    {
        Check.NotNullOrEmpty(mqttClientId, nameof(mqttClientId));

        var match = Regex.Match(mqttClientId, MqttClientIdPatternConsts.TimestampPattern);

        return match.Success && !string.IsNullOrEmpty(match.Groups["timestamp"].Value)
            ? match.Groups["timestamp"].Value
            : null;
    }
}

