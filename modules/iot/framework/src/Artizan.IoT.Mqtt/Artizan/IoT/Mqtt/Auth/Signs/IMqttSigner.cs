namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT 签名器接口
/// </summary>
public interface IMqttSigner
{
    /// <summary>
    /// 生成MQTT连接参数（设备端统一调用入口） 
    /// </summary>
    /// <param name="signParams">签名参数</param>
    /// <param name="secret">
    /// 秘钥
    /// <see cref="MqttAuthType.OneDeviceOneSecret"/> 传入设备秘钥
    /// <see cref="MqttAuthType.OneProductOneSecretPreRegister"/> 传入产品秘钥
    /// <see cref="MqttAuthType.OneProductOneSecretNoPreRegister"/> 传入产品秘钥
    /// </param>
    /// <returns></returns>
    MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret);

    /// <summary>
    /// 验证签名
    /// </summary>
    /// <param name="connectParams">连接参数</param>
    /// <param name="secret">
    /// 秘钥
    /// <see cref="MqttAuthType.OneDeviceOneSecret"/> 传入设备秘钥
    /// <see cref="MqttAuthType.OneProductOneSecretPreRegister"/> 传入产品秘钥
    /// <see cref="MqttAuthType.OneProductOneSecretNoPreRegister"/> 传入产品秘钥
    /// </param>
    /// <returns></returns>
    MqttAuthResult VerifyMqttSign(MqttConnectParams connectParams, string secret);
}
