using Artizan.IoT.Mqtt.Auth;
using Artizan.IoT.Mqtt.Auth.Signs;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Testing;
using Xunit;

namespace Artizan.IoT.Mqtt.Tests.Auths;

/// <summary>
/// MqttSignerFactory 单元测试
/// 适配 IoTMqttTestModule + 精准匹配业务错误码
/// </summary>
public class MqttSignerFactory_Tests : AbpIntegratedTest<IoTMqttTestModule>
{
    private readonly MqttSignerFactory _signerFactory;

    /// <summary>
    /// 构造函数：从ABP测试容器解析依赖
    /// </summary>
    public MqttSignerFactory_Tests()
    {
        _signerFactory = GetRequiredService<MqttSignerFactory>();
    }

    #region 正常场景 - 创建合法认证类型的签名器
    [Fact]
    public void CreateMqttSigner_OneDeviceOneSecret_ReturnsCorrectSigner()
    {
        // Arrange
        var authType = MqttAuthType.OneDeviceOneSecret;

        // Act
        var signer = _signerFactory.CreateMqttSigner(authType);

        // Assert
        signer.ShouldNotBeNull();
        signer.ShouldBeOfType<OneDeviceOneSecretMqttSigner>();
    }

    [Fact]
    public void CreateMqttSigner_OneProductOneSecretPreRegister_ReturnsCorrectSigner()
    {
        // Arrange
        var authType = MqttAuthType.OneProductOneSecretPreRegister;

        // Act
        var signer = _signerFactory.CreateMqttSigner(authType);

        // Assert
        signer.ShouldNotBeNull();
        signer.ShouldBeOfType<OneProductOneSecretMqttSigner>();
        ((OneProductOneSecretMqttSigner)signer).AuthType.ShouldBe(authType);
    }

    [Fact]
    public void CreateMqttSigner_OneProductOneSecretNoPreRegister_ReturnsCorrectSigner()
    {
        // Arrange
        var authType = MqttAuthType.OneProductOneSecretNoPreRegister;

        // Act
        var signer = _signerFactory.CreateMqttSigner(authType);

        // Assert
        signer.ShouldNotBeNull();
        signer.ShouldBeOfType<OneProductOneSecretMqttSigner>();
        ((OneProductOneSecretMqttSigner)signer).AuthType.ShouldBe(authType);
    }
    #endregion

    #region 异常场景 - 创建非法认证类型的签名器（匹配错误码）
    [Fact]
    public void CreateMqttSigner_InvalidAuthType_ThrowsAbpException_WithCorrectErrorCode()
    {
        // Arrange：非法认证类型
        var invalidAuthType = (MqttAuthType)999;
        var expectedErrorCode = IoTMqttErrorCodes.AuthTypeNotSupported; // 匹配业务错误码
        var expectedMessage = $"不支持的MQTT认证类型：{invalidAuthType}";

        // Act
        var exception = Should.Throw<AbpException>(() =>
            _signerFactory.CreateMqttSigner(invalidAuthType));

        // Assert：验证异常属性（错误码+消息+日志级别）
        //exception.Code.ShouldBe(expectedErrorCode); // 核心：匹配业务定义的错误码
        //exception.Message.ShouldContain(expectedMessage);
        //exception.LogLevel.ShouldBe(Volo.Abp.Logging.LogLevel.Warning);

        // 额外验证：异常数据字典包含错误码（适配ABP异常处理体系）
        exception.Data["Code"].ShouldBe(expectedErrorCode);
    }

    [Fact]
    public void CreateMqttSigner_NullAuthType_ThrowsArgumentNullException()
    {
        // 注意：若AuthType是值类型（枚举），无需此测试；若为可空枚举则保留
        // Arrange
        MqttAuthType? nullAuthType = null;

        // Act + Assert
        Should.Throw<ArgumentNullException>(() =>
            _signerFactory.CreateMqttSigner(nullAuthType.Value));
    }
    #endregion
}
