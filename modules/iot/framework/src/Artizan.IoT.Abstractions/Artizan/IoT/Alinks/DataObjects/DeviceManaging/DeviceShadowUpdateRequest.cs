using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.DeviceManaging;

/// <summary>
/// 设备影子更新请求
/// Method：thing.shadow.update
/// </summary>
public class DeviceShadowUpdateRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.shadow.update";

    [JsonPropertyName("params")]
    public DeviceShadowUpdateParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/shadow/update";
    }
}

/// <summary>
/// 影子更新参数
/// </summary>
public class DeviceShadowUpdateParams
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("reported")]
    public Dictionary<string, object> Reported { get; set; } = new();

    [JsonPropertyName("desired")]
    public Dictionary<string, object> Desired { get; set; } = new();

    [JsonPropertyName("delete")]
    public List<string> Delete { get; set; } = new(); // 需要删除的属性键
}