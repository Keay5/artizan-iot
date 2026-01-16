using Artizan.IoT.Mqtt.Auth.Signs;

namespace Artizan.IoT.Mqtt.Auth;

/// <summary>
/// 通过MQTT签名进行认证的管理器接口
/// </summary>
public interface IMqttSignAuthManager
{
    /// <summary>
    /// 生成MQTT连接参数（设备端统一调用入口） 
    /// </summary>
    /// <param name="signParams"></param>
    /// <returns></returns>
    MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret);

    /// <summary>
    /// 解析MQTT连接参数，提取签名参数
    /// </summary>
    /// <param name="mqttClientId">MQTT ClientID</param>
    /// <param name="mqttUserName">MQTT UserName</param>
    /// <returns></returns>
    MqttAuthResult ParseMqttSignParams(string mqttClientId, string mqttUserName);

    /// <summary>
    /// 验证MQTT签名
    /// </summary>
    MqttAuthResult VerifyMqttSign(MqttConnectParams connectParams, string secret);
}
