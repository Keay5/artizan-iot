namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT签名核心参数（解析/构建ClientId、生成/验证签名时使用）
/// 一机一密：ClientId 格式 ProductKey.DeviceName|参数|，支持 securemode=2（TCP）/3（TLS）
/// 一型一密预注册：ClientId 格式 DeviceName|参数|，securemode 固定 = 2
/// 一型一密免预注册：ClientId 格式 DeviceName|参数|，securemode 固定 = -2
/// </summary>
public class MqttSignParams
{
    /// <summary>
    /// 认证类型
    /// </summary>
    public MqttAuthType AuthType { get; set; }

    ///// <summary>
    ///// 再三考虑，不打算使用SecureMode，降低复杂度。
    ///// 安全模式（一机一密=2/3, 一型一密预注册=2，免预注册=-2，）
    ///// </summary>
    //public int SecureMode { get; set; }

    /// <summary>
    /// 签名算法
    /// </summary>
    public MqttSignMethod SignMethod { get; set; }

    /// <summary>
    /// 产品Key（一机一密必填）, （设备预烧录）
    /// </summary>
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（所有认证类型必填）, （设备预烧录）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳（毫秒级，防止重放攻击）
    /// DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// 随机数（一型一密必填，32位以内字符串）
    /// </summary>
    public string? Random { get; set; }

    /// <summary>
    /// 实例ID（企业版IoT实例必填）
    /// </summary>
    public string? InstanceId { get; set; }
}