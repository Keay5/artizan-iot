using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备影子获取请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/forwarding-of-device-shadow-data
/// Method：thing.shadow.get
/// </summary>
public class DeviceShadowGetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.shadow.get";

    [JsonPropertyName("params")]
    public DeviceShadowGetParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/shadow/get";
    }
}

/// <summary>
/// 影子获取参数
/// </summary>
public class DeviceShadowGetParams
{
    [JsonPropertyName("version")]
    public int? Version { get; set; } // 为空则获取最新版本
}

/// <summary>
/// 设备影子数据
/// </summary>
public class DeviceShadowData
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("reported")]
    public Dictionary<string, object> Reported { get; set; } = new(); // 上报属性

    [JsonPropertyName("desired")]
    public Dictionary<string, object> Desired { get; set; } = new(); // 期望属性

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}




