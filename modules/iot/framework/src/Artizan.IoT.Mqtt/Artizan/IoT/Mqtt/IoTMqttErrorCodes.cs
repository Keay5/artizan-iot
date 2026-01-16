namespace Artizan.IoT;

public static class IoTMqttErrorCodes
{
    public const string Namespace = "IoTMqtt";

    //Add your business exception error codes here...
    public const string DefaultError = $"{Namespace}:DefaultError";

    //-----------------------------产品----------------------------------------------------------
    public const string ProductKeyInvalid = $"{Namespace}:Product:001";
    public const string ProductSecrtCanNotBeNull = $"{Namespace}:Device:002";

    //-----------------------------设备----------------------------------------------------------
    public const string DeviceNameInvalid = $"{Namespace}:Device:001";
    public const string DeviceNameCanNotBeNull = $"{Namespace}:Device:002";
    public const string DeviceSecrtCanNotBeNull = $"{Namespace}:Device:003";

    //-----------------------------MQTT 认证----------------------------------------------------------
    public const string ClientIdFormatInvalid = $"{Namespace}:MqttAuth:001";         // ClientId格式非法
    public const string ProductKeyCanNotBeNull = $"{Namespace}:MqttAuth:002";        // ProductKey 为空
    public const string UserNameCanNotBeNull = $"{Namespace}:MqttAuth:003";          // DeviceName 为空
    public const string AuthTypeInvalid = $"{Namespace}:MqttAuth:004";               // 认证类型无效
    public const string AuthTypeMismatch = $"{Namespace}:MqttAuth:005";              // 认证类型不匹配
    public const string SignParamsInvalid = $"{Namespace}:MqttAuth:006";             // 签名参数无效
    public const string AuthTypeNotSupported = $"{Namespace}:MqttAuth:007";          // 不支持的认证类型
    public const string OneProductOneSecretAuthParamRandomCanNotBeNull = $"{Namespace}:MqttAuth:008";   // 一型一密认证必须包含随机数（random参数）
    public const string SignatureVerifyFailed = $"{Namespace}:MqttAuth:009";          // 签名验证失败

}

