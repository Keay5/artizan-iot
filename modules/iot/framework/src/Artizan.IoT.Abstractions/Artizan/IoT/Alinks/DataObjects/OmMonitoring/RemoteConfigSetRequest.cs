using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程配置设置请求（云端→设备）
/// Method：thing.service.config.set
/// Topic模板：/sys/${productKey}/${deviceName}/thing/service/config/set
/// </summary>
public class RemoteConfigSetRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.service.config.set";

    [JsonPropertyName("params")]
    public RemoteConfigSetParams Params { get; set; } = new();

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
        return $"/sys/{productKey}/{deviceName}/thing/service/config/set";
    }

    /// <summary>
    /// 校验配置参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (Params.Configs == null || !Params.Configs.Any())
        {
            return ValidateResult.Failed("配置键值对（Configs）不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.Version))
        {
            return ValidateResult.Failed("配置版本（Version）不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 远程配置设置参数
/// </summary>
public class RemoteConfigSetParams
{
    /// <summary>
    /// 配置版本（需唯一，建议时间戳）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    /// <summary>
    /// 配置键值对
    /// </summary>
    [JsonPropertyName("configs")]
    public Dictionary<string, string> Configs { get; set; } = new();

    /// <summary>
    /// 是否强制更新（忽略版本校验）
    /// </summary>
    [JsonPropertyName("force")]
    public bool Force { get; set; } = false;

    /// <summary>
    /// 配置生效时间（UTC毫秒级，0表示立即生效）
    /// </summary>
    [JsonPropertyName("effectiveTime")]
    public long EffectiveTime { get; set; } = 0;
}
