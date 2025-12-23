using Artizan.IoT.Mqtts;
using Artizan.IoT.Mqtts.Signs;
using Shouldly;
using System;
using Xunit;

namespace Artizan.IoT.Abstractions.Tests.Mqtts.Signs;

public class MqttSignAuthTests
{
    #region 测试基础数据
    // 一机一密测试数据
    private const string Odos_ProductKey = "a1B2c3D4e5";
    private const string Odos_DeviceName = "Device_001";
    private const string Odos_DeviceSecret = "sEcReT1234567890abcdef";

    // 一型一密测试数据
    private const string Opos_ProductKey = "f6G7h8I9j0";
    private const string Opos_PreRegDeviceName = "PreReg_Device_002";
    private string Opos_NoPreRegDeviceName = "NoPreReg_Device_" + Guid.NewGuid().ToString("N").Substring(0, 10);
    private const string Opos_ProductSecret = "pRoDuCtSeCrEt0987654321";
    private const string Opos_Random = "rAnDoM1234567890abcdef";

    #endregion

    #region 一机一密测试
    [Fact]
    public void OneDeviceOneSecret_GenerateConnectParams_ShouldMatchStandard()
    {
        // Arrange
        var signTool = new OneDeviceOneSecretMqttSign();

        // Act
        var (clientId, userName, password, param) = signTool.GenerateConnectParams(
            Odos_ProductKey, Odos_DeviceName, Odos_DeviceSecret, secureMode: MqttSecureModeConstants.Tls);

        // Assert
        // ClientId校验：ProductKey.DeviceName|参数|
        clientId.ShouldStartWith($"{Odos_ProductKey}.{Odos_DeviceName}|");
        clientId.ShouldEndWith("|");
        clientId.ShouldContain($"authType={(int)MqttAuthType.OneDeviceOneSecret}");
        clientId.ShouldContain($"secureMode={MqttSecureModeConstants.Tls}");

        // UserName校验：DeviceName&ProductKey
        userName.ShouldBe($"{Odos_DeviceName}&{Odos_ProductKey}");

        // 参数校验
        param.AuthType.ShouldBe(MqttAuthType.OneDeviceOneSecret);
        param.SecureMode.ShouldBe(MqttSecureModeConstants.Tls);
        param.ProductKey.ShouldBe(Odos_ProductKey);
        param.DeviceName.ShouldBe(Odos_DeviceName);
        param.Timestamp.ShouldBeNullOrEmpty();
        password.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void OneDeviceOneSecret_ValidateSign_Success_WhenParamsCorrect()
    {
        // Arrange
        var obosSign = new OneDeviceOneSecretMqttSign();
        var (clientId, userName, password, _) = obosSign.GenerateConnectParams(
            Odos_ProductKey, Odos_DeviceName, Odos_DeviceSecret);

        // Act
        var result = obosSign.ValidateSign(clientId, userName, password, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Code.ShouldBe(0);
        result.Message.ShouldBe("认证通过");
        result.Params.ProductKey.ShouldBe(Odos_ProductKey);
    }

    [Fact]
    public void OneDeviceOneSecret_ValidateSign_Fail_WhenClientIdMissingProductKey()
    {
        // Arrange
        var odosSign = new OneDeviceOneSecretMqttSign();
        var (_, userName, password, _) = 
            odosSign.GenerateConnectParams(Odos_ProductKey, Odos_DeviceName, Odos_DeviceSecret);
        var invalidClientId = $"{Odos_DeviceName}|authType=1,secureMode=2|"; // 缺少ProductKey前缀

        // Act
        var result = odosSign.ValidateSign(invalidClientId, userName, password, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(2);
        result.Message.ShouldContain("ClientId前缀格式非法");
    }
    #endregion

    #region 一型一密预注册测试
    [Fact]
    public void OneProductOneSecretPreRegister_SecureMode_MustBe_2()
    {
        // Arrange
        var oposSign = new OneProductOneSecretMqttSign(MqttAuthType.OneProductOneSecretPreRegister);

        // Act：尝试传入其他安全模式（会被忽略）
        var (clientId, _, _, param) = oposSign.GenerateConnectParams(
            Opos_ProductKey, Opos_PreRegDeviceName, Opos_ProductSecret, Opos_Random, secureMode: 3);

        // Assert
        param.SecureMode.ShouldBe(MqttSecureModeConstants.Tcp); // 强制为2
        clientId.ShouldContain($"secureMode={MqttSecureModeConstants.Tcp}");
    }

    [Fact]
    public void OneProductOneSecretPreRegister_ValidateSign_Success_WhenParamsCorrect()
    {
        // Arrange
        var oposSign = new OneProductOneSecretMqttSign(MqttAuthType.OneProductOneSecretPreRegister);
        var (clientId, userName, password, _) = oposSign.GenerateConnectParams(
            Opos_ProductKey, Opos_PreRegDeviceName, Opos_ProductSecret, Opos_Random);

        // Act
        var result = oposSign.ValidateSign(clientId, userName, password, Opos_ProductSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Params.AuthType.ShouldBe(MqttAuthType.OneProductOneSecretPreRegister);
        result.Params.SecureMode.ShouldBe(MqttSecureModeConstants.Tcp);
    }
    #endregion

    #region 一型一密免预注册测试
    [Fact]
    public void OneProductOneSecretNoPreRegister_SecureMode_MustBe_Negative2()
    {
        // Arrange
        var oposSign = new OneProductOneSecretMqttSign(MqttAuthType.OneProductOneSecretNoPreRegister);

        // Act：尝试传入其他安全模式（会被忽略）
        var (clientId, _, _, param) = oposSign.GenerateConnectParams(
            Opos_ProductKey, Opos_NoPreRegDeviceName, Opos_ProductSecret, Opos_Random, secureMode: 2);

        // Assert
        param.SecureMode.ShouldBe(MqttSecureModeConstants.OneProductNoPreRegister); // 强制为-2
        clientId.ShouldContain($"secureMode={MqttSecureModeConstants.OneProductNoPreRegister}");
    }

    [Fact]
    public void OneProductOneSecretNoPreRegister_ValidateSign_Fail_WhenSecureModeNotNegative2()
    {
        // Arrange
        var oposSign = new OneProductOneSecretMqttSign(MqttAuthType.OneProductOneSecretNoPreRegister);
        var (clientId, userName, password, _) = oposSign.GenerateConnectParams(
            Opos_ProductKey, Opos_NoPreRegDeviceName, Opos_ProductSecret, Opos_Random);

        // 构造非法ClientId（修改secureMode为2）
        var invalidClientId = clientId.Replace(
            $"secureMode={MqttSecureModeConstants.OneProductNoPreRegister}",
            $"secureMode={MqttSecureModeConstants.Tcp}");

        // Act
        var result = oposSign.ValidateSign(invalidClientId, userName, password, Opos_ProductSecret);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Message.ShouldContain($"安全模式不匹配，{MqttAuthType.OneProductOneSecretNoPreRegister}强制要求：{MqttSecureModeConstants.OneProductNoPreRegister}");
    }
    #endregion

    #region MqttAuthManager 统一入口测试
    [Fact]
    public void MqttAuthManager_OneProductOneSecretNoPreRegister_GenerateConnectParams_ShouldDelegateCorrectly()
    {
        // Arrange：测试一型一密免预注册
        var authType = MqttAuthType.OneProductOneSecretNoPreRegister;

        // Act
        var (clientId, userName, _, param) =
            MqttSignAuthManager.GenerateConnectParams(authType, Opos_ProductKey, Opos_NoPreRegDeviceName, Opos_ProductSecret, Opos_Random);

        // Assert
        clientId.ShouldStartWith(Opos_NoPreRegDeviceName);
        clientId.ShouldNotContain(Opos_ProductKey);
        userName.ShouldBe($"{Opos_NoPreRegDeviceName}&{Opos_ProductKey}");
        param.AuthType.ShouldBe(authType);
        param.SecureMode.ShouldBe(MqttSecureModeConstants.OneProductNoPreRegister);
    }

    [Fact]
    public void MqttAuthManager_OneDeviceOneSecret_ValidateSign_ShouldDelegateCorrectly()
    {
        // -----------------------------------一、测试一机一密(无 timeStamp、无random)-----------------------------------
        // Arrange
        var authType = MqttAuthType.OneDeviceOneSecret;
        var (mqttClientId, mqttUserName, mqttPassword, _) =
            MqttSignAuthManager.GenerateConnectParams(authType, Odos_ProductKey, Odos_DeviceName, Odos_DeviceSecret);

        // Assert
        mqttClientId.ShouldBe("a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256|");
        mqttUserName.ShouldBe("Device_001&a1B2c3D4e5");

        // Act
        var result = MqttSignAuthManager.ValidateSign(mqttClientId, mqttUserName, mqttPassword, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // -----------------------------------二、测试一机一密(有timeStamp、无random)-----------------------------------
        // Arrange
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        (mqttClientId, mqttUserName, mqttPassword, _) =
            MqttSignAuthManager.GenerateConnectParams(
                authType, 
                Odos_ProductKey, 
                Odos_DeviceName,
                Odos_DeviceSecret, 
                timestamp: timeStamp);

        // Assert
        mqttClientId.ShouldBe($"a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256,timestamp={timeStamp}|");
        mqttUserName.ShouldBe("Device_001&a1B2c3D4e5");

        // Act
        result = MqttSignAuthManager.ValidateSign(mqttClientId, mqttUserName, mqttPassword, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // -----------------------------------三、测试一机一密(无timeStamp、有random)-----------------------------------
        // Arrange
        (mqttClientId, mqttUserName, mqttPassword, _) = MqttSignAuthManager.GenerateConnectParams(
            authType, 
            Odos_ProductKey, 
            Odos_DeviceName, 
            Odos_DeviceSecret, 
            random: "0987654321");

        // Assert
        mqttClientId.ShouldBe("a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256,random=0987654321|");
        mqttUserName.ShouldBe("Device_001&a1B2c3D4e5");

        // Act
        result = MqttSignAuthManager.ValidateSign(mqttClientId, mqttUserName, mqttPassword, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // ----------------------------------- 四、测试一机一密(有timeStamp、有random) -----------------------------------
        // Arrange
        timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        (mqttClientId, mqttUserName, mqttPassword, _) = MqttSignAuthManager.GenerateConnectParams(
            authType, 
            Odos_ProductKey, 
            Odos_DeviceName, 
            Odos_DeviceSecret, 
            timestamp: timeStamp, 
            random: "0987654321");

        // Assert
        mqttClientId.ShouldBe($"a1B2c3D4e5.Device_001|authType=1,secureMode=2,signMethod=HmacSha256,random=0987654321,timestamp={timeStamp}|");
        mqttUserName.ShouldBe("Device_001&a1B2c3D4e5");

        // Act
        result = MqttSignAuthManager.ValidateSign(mqttClientId, mqttUserName, mqttPassword, Odos_DeviceSecret);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
    #endregion
}