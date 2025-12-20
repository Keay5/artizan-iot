using Artizan.IoT.Mqtts.Signs;
using Shouldly;
using System;
using Xunit;

namespace Artizan.IoT.Abstractions.Tests.Mqtts.Signs;

public class AliIoTMqttPasswordGeneratorTests
{
    private readonly AliIoTMqttPasswordGenerator _generator = new AliIoTMqttPasswordGenerator();

    [Fact]
    public void GenerateUsername_ShouldReturnCorrectFormat()
    {
        // Arrange
        const string deviceName = "testDevice";
        const string productKey = "testProductKey";

        // Act
        var username = _generator.GenerateUsername(deviceName, productKey);

        // Assert
        username.ShouldBe($"{deviceName}&{productKey}");
    }

    [Fact]
    public void GenerateClientId_WithTimestamp_ShouldContainAllParameters()
    {
        // Arrange
        const string clientId = "testClientId";
        const string productKey = "testPK";
        const string deviceName = "testDN";
        const long timestamp = 1620000000000;
        var signMethod = AliIoTMqttPasswordGenerator.SignMethod.HmacSha1;

        // Act
        var mqttClientId = _generator.GenerateClientId(clientId, productKey, deviceName, timestamp, signMethod);

        // Assert
        mqttClientId.ShouldContain(clientId);
        mqttClientId.ShouldContain("securemode=2");
        mqttClientId.ShouldContain("signmethod=hmacsha1");
        mqttClientId.ShouldContain($"timestamp={timestamp}");
    }

    [Fact]
    public void GeneratePassword_WithHmacSha1_ShouldMatchExpectedValue()
    {
        // Arrange - 使用已知值进行测试
        const string productKey = "a1BcDeFgHiJ";
        const string deviceName = "testDevice123";
        const string deviceSecret = "testSecret456";
        const string clientId = "client123";
        const long timestamp = 1764636041723;

        // 计算预期值：通过原JS代码计算得到
        const string expectedPassword = "9E6CE2A4E0740598CBB5C5BE276A7167AF8B2C53";

        // Act
        var password = _generator.GeneratePassword(
            productKey,
            deviceName,
            deviceSecret,
            clientId,
            timestamp,
            AliIoTMqttPasswordGenerator.SignMethod.HmacSha1);

        // Assert
        password.ShouldBe(expectedPassword);
    }

    [Fact]
    public void GeneratePassword_WithHmacMd5_ShouldMatchExpectedValue()
    {
        // Arrange
        const string productKey = "a1BcDeFgHiJ";
        const string deviceName = "testDevice123";
        const string deviceSecret = "testSecret456";
        const string clientId = "client123";
        const long timestamp = 1764636041723;

        // 计算预期值：通过原JS代码计算得到
        const string expectedPassword = "76D326582AC69BCCE400F19E85FED1AB";

        // Act
        var password = _generator.GeneratePassword(
            productKey,
            deviceName,
            deviceSecret,
            clientId,
            timestamp,
            AliIoTMqttPasswordGenerator.SignMethod.HmacMd5);

        // Assert
        password.ShouldBe(expectedPassword);
    }

    [Fact]
    public void GeneratePassword_WithoutTimestamp_ShouldExcludeTimestampInContent()
    {
        // Arrange
        const string productKey = "testPK";
        const string deviceName = "testDN";
        const string deviceSecret = "secret";
        const string clientId = "client";

        // Act
        var passwordWithTimestamp = _generator.GeneratePassword(
            productKey, deviceName, deviceSecret, clientId, 1764636041723, AliIoTMqttPasswordGenerator.SignMethod.HmacSha1);
        var passwordWithoutTimestamp = _generator.GeneratePassword(
            productKey, deviceName, deviceSecret, clientId, null, AliIoTMqttPasswordGenerator.SignMethod.HmacSha1);

        // Assert
        passwordWithTimestamp.ShouldNotBe(passwordWithoutTimestamp);
    }

    [Fact]
    public void GeneratePassword_WithInvalidSignMethod_ShouldThrowException()
    {
        // Arrange
        const string productKey = "testPK";
        const string deviceName = "testDN";
        const string deviceSecret = "secret";
        const string clientId = "client";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _generator.GeneratePassword(
                productKey, deviceName, deviceSecret, clientId, null, (AliIoTMqttPasswordGenerator.SignMethod)999));
    }
}
