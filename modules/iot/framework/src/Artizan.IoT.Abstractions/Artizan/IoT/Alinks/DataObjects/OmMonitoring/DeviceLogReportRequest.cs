using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 设备日志上报请求
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/device-log-reporting
/// Method：thing.event.log.post
/// Topic模板：/sys/${productKey}/${deviceName}/thing/event/log/post
/// </summary>
public class DeviceLogReportRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.event.log.post";

    [JsonPropertyName("params")]
    public DeviceLogReportParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/event/log/post";
    }

    /// <summary>
    /// 校验日志参数合法性
    /// </summary>
    public ValidateResult Validate()
    {
        var validLevels = new List<string> { "debug", "info", "warn", "error", "fatal" };
        if (!validLevels.Contains(Params.LogLevel.ToLower()))
        {
            return ValidateResult.Failed($"日志级别（LogLevel）必须是：{string.Join("/", validLevels)}");
        }
        if (string.IsNullOrWhiteSpace(Params.Content))
        {
            return ValidateResult.Failed("日志内容（Content）不能为空");
        }
        if (Params.Content.Length > 2048)
        {
            return ValidateResult.Failed("日志内容长度不能超过2048字符");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 设备日志上报参数
/// </summary>
public class DeviceLogReportParams
{
    /// <summary>
    /// 日志级别（debug/info/warn/error/fatal）
    /// </summary>
    [JsonPropertyName("level")]
    public string LogLevel { get; set; } = "info";

    /// <summary>
    /// 日志内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 日志模块（如ota/network/mqtt/config）
    /// </summary>
    [JsonPropertyName("module")]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 日志产生时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 自定义标签（用于日志分类筛选）
    /// </summary>
    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// 日志上下文（可选，如设备当前状态）
    /// </summary>
    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;
}


