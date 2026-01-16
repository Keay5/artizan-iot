namespace Artizan.IoT.Mqtt.Auth;

/// <summary>
/// MQTT 认证类型（一机一密、一型一密等）
/// </summary>
public enum MqttAuthType
{
    /// <summary>
    /// 一机一密（设备预烧录ProductKey+DeviceName+DeviceSecret）
    /// 文档：https://help.aliyun.com/zh/iot/user-guide/unique-certificate-per-device-verification
    /// </summary>
    OneDeviceOneSecret = 1,
    /// <summary>
    /// 一型一密预注册（平台预注册DeviceName，设备预烧录ProductKey+ProductSecret+DeviceName）
    /// 文档：https://help.aliyun.com/zh/iot/user-guide/unique-certificate-per-product-verification
    /// </summary>
    OneProductOneSecretPreRegister = 2,

    /// <summary>
    /// 一型一密免预注册（平台不预注册DeviceName，设备预烧录ProductKey+ProductSecret，DeviceName由设备生成）
    /// 文档：https://help.aliyun.com/zh/iot/user-guide/mqtt-based-dynamic-registration
    /// </summary>
    OneProductOneSecretNoPreRegister = 3
}
