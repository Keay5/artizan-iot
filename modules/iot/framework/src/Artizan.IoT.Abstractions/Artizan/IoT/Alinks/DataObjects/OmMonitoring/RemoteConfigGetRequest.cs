using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程配置获取请求（设备→云端）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/remote-configuration-1
/// Method：thing.service.config.get
/// Topic模板：/sys/${productKey}/${deviceName}/thing/service/config/get
/// </summary>
public class RemoteConfigGetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.service.config.get";

    [JsonPropertyName("params")]
    public RemoteConfigGetParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/service/config/get";
    }
}

/// <summary>
/// 远程配置获取参数
/// </summary>
public class RemoteConfigGetParams
{
    /// <summary>
    /// 配置键列表（为空则获取所有配置）
    /// </summary>
    [JsonPropertyName("keys")]
    public List<string> Keys { get; set; } = new();

    /// <summary>
    /// 配置版本（为空则获取最新版本）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

