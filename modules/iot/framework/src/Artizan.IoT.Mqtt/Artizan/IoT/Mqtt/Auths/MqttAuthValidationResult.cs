namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// 认证结果包装类
/// 用于封装认证过程的结果信息
/// </summary>
public record MqttAuthValidationResult(bool IsSuccess, MqttAuthParams? Params, string? Message);

