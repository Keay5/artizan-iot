using System;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// 认证失败异常（用于触发Polly熔断）
/// </summary>
public class MqttAuthenticationFailedException : Exception
{
    public MqttAuthenticationFailedException(string message) : base(message) { }
    public MqttAuthenticationFailedException(string message, Exception innerException) : base(message, innerException) { }
}
