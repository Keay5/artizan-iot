namespace Artizan.IoT.Mqtts.Signs;

/// <summary>
/// 认证类型扩展方法
/// </summary>
public static class MqttAuthExtensions
{
    /// <summary>
    /// 判断是否为一型一密认证类型
    /// </summary>
    public static bool IsOneProductOnSecretAuth(this MqttAuthType authType)
    {
        return authType is MqttAuthType.OneProductOneSecretPreRegister or MqttAuthType.OneProductOneSecretNoPreRegister;
    }

    /// <summary>
    /// 判断是否为一机一密认证类型
    /// </summary>
    public static bool IsOneDeviceOnSecretAuth(this MqttAuthType authType)
    {
        return authType == MqttAuthType.OneDeviceOneSecret;
    }

    /// <summary>
    /// 获取认证类型对应的固定安全模式（严格遵循文档）
    /// </summary>
    public static int GetFixedSecureMode(this MqttAuthType authType)
    {
        return authType switch
        {
            MqttAuthType.OneProductOneSecretPreRegister => MqttSecureModeConstants.Tcp, // 固定2
            MqttAuthType.OneProductOneSecretNoPreRegister => MqttSecureModeConstants.OneProductNoPreRegister, // 固定-2
            MqttAuthType.OneDeviceOneSecret => MqttSecureModeConstants.Tcp, // 默认2，支持手动指定3
            _ => MqttSecureModeConstants.Tcp
        };
    }
}
