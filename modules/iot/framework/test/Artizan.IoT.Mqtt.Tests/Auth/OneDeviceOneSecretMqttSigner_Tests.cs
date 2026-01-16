using Artizan.IoT.Mqtt.Auth;
using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Mqtt.Exceptions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Artizan.IoT.Mqtt.Tests.Auth;

/// <summary>
/// 一机一密签名器单元测试（匹配最终版业务代码）
/// 覆盖：参数校验、ClientId构建、签名生成/验证、所有异常场景
/// </summary>
public class OneDeviceOneSecretMqttSigner_Tests
{
    // 测试常量（贴合业务格式）
    private const string TestProductKey = "a1b2c3d4e5f6";
    private const string TestDeviceName = "device_test_001";
    private const string TestDeviceSecret = "device_secret_1234567890";
    private const string TestRandom = "rand_888888";
    private const string TestTimestamp = "1735689600000";
    private const string TestInstanceId = "instance_001";

    #region 核心工具方法：构建合法签名参数
    private MqttSignParams BuildValidSignParams(MqttSignMethod signMethod = MqttSignMethod.HmacSha256)
    {
        return new MqttSignParams
        {
            AuthType = MqttAuthType.OneDeviceOneSecret,
            ProductKey = TestProductKey,
            DeviceName = TestDeviceName,
            SignMethod = signMethod,
            Random = TestRandom,
            Timestamp = TestTimestamp,
            InstanceId = TestInstanceId
        };
    }
    #endregion

    #region 1. GenerateMqttConnectParams 测试
    [Theory]
    [InlineData(MqttSignMethod.HmacSha1)]
    [InlineData(MqttSignMethod.HmacSha256)]
    [InlineData(MqttSignMethod.HmacMd5)]
    public void GenerateMqttConnectParams_ValidParams_AllSignMethods_ReturnsCorrectFormat(MqttSignMethod signMethod)
    {
        // Arrange
        var signer = new OneDeviceOneSecretMqttSigner();
        var signParams = BuildValidSignParams(signMethod);

        // Act
        var connectParams = signer.GenerateMqttConnectParams(signParams, TestDeviceSecret);

        // Assert：核心参数格式验证
        connectParams.ShouldNotBeNull();

        // 1. ClientId格式验证（ProductKey.DeviceName|参数列表|）
        connectParams.ClientId.ShouldStartWith($"{TestProductKey}.{TestDeviceName}|");
        connectParams.ClientId.ShouldContain($"authType={(int)MqttAuthType.OneDeviceOneSecret}");
        connectParams.ClientId.ShouldContain($"signMethod={signMethod.ToString()}");
        connectParams.ClientId.ShouldContain($"random={TestRandom}");
        connectParams.ClientId.ShouldContain($"timestamp={TestTimestamp}");
        connectParams.ClientId.ShouldContain($"instanceId={TestInstanceId}");
        connectParams.ClientId.ShouldEndWith("|");

        // 2. UserName格式验证（DeviceName&ProductKey）
        connectParams.UserName.ShouldBe($"{TestDeviceName}&{TestProductKey}");

        // 3. 签名（Password）非空（验证加密逻辑调用）
        connectParams.Password.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateMqttConnectParams_EmptyProductKey_ThrowsMqttResultException()
    {
        // Arrange：空ProductKey（触发错误码：MqttAuth:002）
        var signer = new OneDeviceOneSecretMqttSigner();
        var invalidParams = BuildValidSignParams();
        invalidParams.ProductKey = "";

        // Act + Assert
        var exception = Should.Throw<MqttResultException>(() =>
            signer.GenerateMqttConnectParams(invalidParams, TestDeviceSecret));

        // 验证错误码和异常关联的IoTResult
        exception.IoTResult.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.ProductKeyCanNotBeNull);
        //exception.Code.ShouldBe(IoTMqttErrorCodes.ProductKeyInvalid);
    }

    [Fact]
    public void GenerateMqttConnectParams_EmptyDeviceSecret_ThrowsMqttResultException()
    {
        // Arrange：空设备秘钥（触发错误码：Device:002）
        var signer = new OneDeviceOneSecretMqttSigner();
        var validParams = BuildValidSignParams();

        // Act + Assert
        var exception = Should.Throw<MqttResultException>(() =>
            signer.GenerateMqttConnectParams(validParams, ""));

        exception.IoTResult.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.DeviceSecrtCanNotBeNull);
    }
    #endregion

    #region 2. VerifyMqttSign 测试
    [Theory]
    [InlineData(MqttSignMethod.HmacSha256)]
    [InlineData(MqttSignMethod.HmacMd5)]
    public void VerifyMqttSign_ValidSign_ReturnsSuccess(MqttSignMethod signMethod)
    {
        // Step1：生成合法连接参数
        var signer = new OneDeviceOneSecretMqttSigner();
        var signParams = BuildValidSignParams(signMethod);
        var connectParams = signer.GenerateMqttConnectParams(signParams, TestDeviceSecret);

        // Step2：验证签名
        var verifyResult = signer.VerifyMqttSign(connectParams, TestDeviceSecret);

        // Assert
        verifyResult.Succeeded.ShouldBeTrue();
        verifyResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void VerifyMqttSign_InvalidSign_ReturnsFailed()
    {
        // Step1：生成合法参数，篡改签名
        var signer = new OneDeviceOneSecretMqttSigner();
        var signParams = BuildValidSignParams();
        var connectParams = signer.GenerateMqttConnectParams(signParams, TestDeviceSecret);
        connectParams.Password = "invalid_sign_123456"; // 篡改签名

        // Step2：验证签名
        var verifyResult = signer.VerifyMqttSign(connectParams, TestDeviceSecret);

        // Assert：验证错误码 + 描述
        verifyResult.Succeeded.ShouldBeFalse();
        verifyResult.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.SignatureVerifyFailed);
    }

    [Fact]
    public void VerifyMqttSign_AuthTypeMismatch_ReturnsFailed()
    {
        // Arrange：构造认证类型不匹配的参数（触发错误码：MqttAuth:005）
        var signer = new OneDeviceOneSecretMqttSigner();
        var invalidSignParams = BuildValidSignParams();
        invalidSignParams.AuthType = MqttAuthType.OneProductOneSecretPreRegister; // 一型一密类型

        // 手动构造非法ClientId（携带错误认证类型）
        var connectParams = new MqttConnectParams
        {
            ClientId = $"{TestProductKey}.{TestDeviceName}|authType={(int)MqttAuthType.OneProductOneSecretPreRegister}|",
            UserName = $"{TestDeviceName}&{TestProductKey}",
            Password = "any_sign"
        };

        // Act + Assert：验证抛出异常且错误码匹配
        //var exception = Should.Throw<MqttResultException>(() =>
        //    signer.VerifyMqttSign(connectParams, TestDeviceSecret));

        //exception.IoTResult.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.AuthTypeMismatch);
        //exception.IoTResult.Errors.First().Description.ShouldContain($"预期：{MqttAuthType.OneDeviceOneSecret}");

        var verifyResult = signer.VerifyMqttSign(connectParams, TestDeviceSecret);

        // Assert：验证错误码 + 描述
        verifyResult.Succeeded.ShouldBeFalse();
        verifyResult.Errors.ShouldContain(e =>
            e.Code == IoTMqttErrorCodes.AuthTypeMismatch);
    }

    [Fact]
    public void VerifyMqttSign_InvalidClientIdPrefix_ThrowsMqttResultException()
    {
        // Arrange：ClientId无ProductKey前缀（触发错误码：MqttAuth:001）
        var signer = new OneDeviceOneSecretMqttSigner();
        var connectParams = new MqttConnectParams
        {
            ClientId = $"{TestDeviceName}|authType=1|", // 无ProductKey前缀
            UserName = $"{TestDeviceName}&{TestProductKey}",
            Password = "any_sign"
        };

        var verifyResult = signer.VerifyMqttSign(connectParams, TestDeviceSecret);

        // Assert：验证错误码 + 描述
        verifyResult.Succeeded.ShouldBeFalse();
        verifyResult.Errors.ShouldContain(e =>
            e.Code == IoTMqttErrorCodes.ClientIdFormatInvalid);

        //// Act + Assert
        //var exception = Should.Throw<MqttResultException>(() =>
        //    signer.VerifyMqttSign(connectParams, TestDeviceSecret));

        //exception.IoTResult.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.ClientIdFormatInvalid);
        //exception.IoTResult.Errors.First().Description.ShouldContain("ClientId前缀格式非法（需符合：ProductKey.DeviceName）");
    }
    #endregion

}