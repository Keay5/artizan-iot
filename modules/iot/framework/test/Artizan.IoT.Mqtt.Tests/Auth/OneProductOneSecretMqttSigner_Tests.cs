using Artizan.IoT.Mqtt.Auth;
using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Mqtt.Exceptions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Artizan.IoT.Mqtt.Tests.Auth;

/// <summary>
/// OneProductOneSecretMqttSigner 单元测试
/// </summary>
public class OneProductOneSecretMqttSignerTests
{
    #region 测试常量
    // 合法一型一密认证类型（仅用户枚举中定义的值）
    private readonly MqttAuthType _validAuthTypePreRegister = MqttAuthType.OneProductOneSecretPreRegister;
    private readonly MqttAuthType _validAuthTypeNoPreRegister = MqttAuthType.OneProductOneSecretNoPreRegister;
    private readonly MqttAuthType _invalidAuthType = MqttAuthType.OneDeviceOneSecret; // 一机一密（非一型一密）

    // 有效签名参数：直接 new Artizan.IoT.Results.MqttSignParams
    private readonly MqttSignParams _validSignParams = new MqttSignParams
    {
        ProductKey = "prod123456",
        DeviceName = "device001",
        AuthType = MqttAuthType.OneProductOneSecretPreRegister,
        SignMethod = MqttSignMethod.HmacSha256, 
        Random = "random123456", // 一型一密必填
        Timestamp = "1735689600000",
        InstanceId = "instance001"
    };

    // 缺失Random的非法参数：直接 new Artizan.IoT.Results.MqttSignParams
    private readonly MqttSignParams _signParamsWithoutRandom = new MqttSignParams
    {
        ProductKey = "prod123456",
        DeviceName = "device001",
        AuthType = MqttAuthType.OneProductOneSecretPreRegister,
        SignMethod = MqttSignMethod.HmacSha256,
        Timestamp = "1735689600000"
    };

    // 空ProductKey的非法参数
    private readonly MqttSignParams _signParamsWithEmptyProductKey = new MqttSignParams
    {
        ProductKey = "",
        DeviceName = "device001",
        AuthType = MqttAuthType.OneProductOneSecretPreRegister,
        SignMethod = MqttSignMethod.HmacSha256,
        Random = "random123456"
    };

    // 空DeviceName的非法参数
    private readonly MqttSignParams _signParamsWithEmptyDeviceName = new MqttSignParams
    {
        ProductKey = "prod123456",
        DeviceName = "",
        AuthType = MqttAuthType.OneProductOneSecretPreRegister,
        SignMethod = MqttSignMethod.HmacSha256,
        Random = "random123456"
    };

    // 产品秘钥常量（对应用户错误码）
    private const string _validProductSecret = "testSecret123456";
    private const string _emptyProductSecret = "";
    #endregion

    #region 1. 构造函数测试
    /// <summary>
    /// 场景：一型一密预注册类型 → 成功实例化
    /// </summary>
    [Fact]
    public void Constructor_OneProductOneSecretPreRegister_ShouldCreateInstance()
    {
        // Act
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Assert
        signer.ShouldNotBeNull();
        signer.AuthType.ShouldBe(_validAuthTypePreRegister);
    }

    /// <summary>
    /// 场景：一型一密免预注册类型 → 成功实例化
    /// </summary>
    [Fact]
    public void Constructor_OneProductOneSecretNoPreRegister_ShouldCreateInstance()
    {
        // Act
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypeNoPreRegister);

        // Assert
        signer.ShouldNotBeNull();
        signer.AuthType.ShouldBe(_validAuthTypeNoPreRegister);
    }

    /// <summary>
    /// 场景：传入一机一密类型 → 抛出 ArgumentException
    /// </summary>
    [Fact]
    public void Constructor_OneDeviceOneSecret_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new OneProductOneSecretMqttSigner(_invalidAuthType);

        // Assert
        var ex = act.ShouldThrow<ArgumentException>(); 
        ex.Message.ShouldContain($"不支持的认证类型：{_invalidAuthType}，仅支持一型一密相关类型");
    }
    #endregion

    #region 2. GenerateMqttConnectParams 测试
    /// <summary>
    /// 场景：有效参数 → 生成符合格式的 MQTT 连接参数
    /// </summary>
    [Fact]
    public void GenerateMqttConnectParams_ValidParams_ShouldReturnCorrectParams()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Act
        var connectParams = signer.GenerateMqttConnectParams(_validSignParams, _validProductSecret);

        // Assert（严格匹配 Build 逻辑）
        // ClientId 格式：ProductKey.DeviceName|参数列表|
        connectParams.ClientId.ShouldStartWith($"{_validSignParams.ProductKey}.{_validSignParams.DeviceName}|");
        connectParams.ClientId.ShouldEndWith("|");
        connectParams.ClientId.ShouldContain($"authType={(int)_validSignParams.AuthType}");
        connectParams.ClientId.ShouldContain($"signMethod={_validSignParams.SignMethod}");
        connectParams.ClientId.ShouldContain($"random={_validSignParams.Random}");
        connectParams.ClientId.ShouldContain($"timestamp={_validSignParams.Timestamp}");
        connectParams.ClientId.ShouldContain($"instanceId={_validSignParams.InstanceId}");

        // UserName 格式：DeviceName&ProductKey
        connectParams.UserName.ShouldBe($"{_validSignParams.DeviceName}&{_validSignParams.ProductKey}");

        // Password 非空（签名结果）
        connectParams.Password.ShouldNotBeNullOrEmpty();
    }

    public void GenerateMqttConnectParams_AuthTypeMismatch_ShouldThrowMqttResultException()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister); // OneProductOneSecretPreRegister
        // 生成 免预注册 类型的连接参数
        var mismatchSignParams = new MqttSignParams
        {
            ProductKey = "prod123456",
            DeviceName = "device001",
            AuthType = MqttAuthType.OneProductOneSecretNoPreRegister, // 与签名器类型(OneProductOneSecretPreRegister)不匹配
            SignMethod = MqttSignMethod.HmacSha256,
            Random = "random123456"
        };

        // Act
        MqttConnectParams mismatchConnectParams;
        Action act = () => mismatchConnectParams = signer.GenerateMqttConnectParams(mismatchSignParams, _validProductSecret);

        // Assert
        var exception = act.ShouldThrow<MqttResultException>();
        exception.Code.ShouldBe(IoTMqttErrorCodes.AuthTypeMismatch);
    }

    /// <summary>
    /// 场景：空产品秘钥 → 抛出 MqttResultException
    /// 注意：用户错误码拼写错误 ProductSecrtCanNotBeNull
    /// </summary>
    [Fact]
    public void GenerateMqttConnectParams_EmptyProductSecret_ShouldThrowMqttResultException()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Act
        Action act = () => signer.GenerateMqttConnectParams(_validSignParams, _emptyProductSecret);

        // Assert
        var exception = act.ShouldThrow<MqttResultException>();
        exception.Code.ShouldBe(IoTMqttErrorCodes.ProductSecrtCanNotBeNull);
    }

    /// <summary>
    /// 场景：缺失 Random 参数 → 抛出 MqttResultException
    /// </summary>
    [Fact]
    public void GenerateMqttConnectParams_WithoutRandom_ShouldThrowMqttResultException()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Act
        Action act = () => signer.GenerateMqttConnectParams(_signParamsWithoutRandom, _validProductSecret);

        // Assert
        var exception = act.ShouldThrow<MqttResultException>();
        exception.Code.ShouldBe(IoTMqttErrorCodes.OneProductOneSecretAuthParamRandomCanNotBeNull);
        exception.Message.ShouldBe("一型一密认证必须包含随机数（random参数）");
    }

    /// <summary>
    /// 场景：空 ProductKey → 抛出 MqttResultException
    /// </summary>
    [Fact]
    public void GenerateMqttConnectParams_EmptyProductKey_ShouldThrowMqttResultException()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Act
        Action act = () => signer.GenerateMqttConnectParams(_signParamsWithEmptyProductKey, _validProductSecret);

        // Assert
        var exception = act.ShouldThrow<MqttResultException>();
        exception.Code.ShouldBe(IoTMqttErrorCodes.ProductKeyCanNotBeNull);
    }

    /// <summary>
    /// 场景：空 DeviceName → 抛出 MqttResultException
    /// </summary>
    [Fact]
    public void GenerateMqttConnectParams_EmptyDeviceName_ShouldThrowMqttResultException()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);

        // Act
        Action act = () => signer.GenerateMqttConnectParams(_signParamsWithEmptyDeviceName, _validProductSecret);

        // Assert
        var exception = act.ShouldThrow<MqttResultException>();
        exception.Code.ShouldBe(IoTMqttErrorCodes.DeviceNameInvalid);
    }
    #endregion

    #region 3. VerifyMqttSign 测试
    /// <summary>
    /// 场景：有效签名 → 验证成功
    /// </summary>
    [Fact]
    public void VerifyMqttSign_ValidSign_ShouldReturnSuccess()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);
        // 直接使用 MqttSignParams 生成合法连接参数
        var validConnectParams = signer.GenerateMqttConnectParams(_validSignParams, _validProductSecret);

        // Act
        var result = signer.VerifyMqttSign(validConnectParams, _validProductSecret);

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// 场景：错误产品秘钥 → 验证失败
    /// </summary>
    [Fact]
    public void VerifyMqttSign_WrongSecret_ShouldReturnFailed()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);
        var validConnectParams = signer.GenerateMqttConnectParams(_validSignParams, _validProductSecret);
        const string wrongSecret = "wrongSecret654321";

        // Act
        var result = signer.VerifyMqttSign(validConnectParams, wrongSecret);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors.First().Code.ShouldBe(IoTMqttErrorCodes.SignatureVerifyFailed);
    }

    /// <summary>
    /// 场景：非法 ClientId 格式 → 验证失败
    /// </summary>
    [Fact]
    public void VerifyMqttSign_InvalidClientId_ShouldReturnFailed()
    {
        // Arrange
        var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);
        // 构造非法 ClientId 的连接参数
        var invalidConnectParams = new MqttConnectParams
        {
            ClientId = "invalidClientId", // 不符合 DeviceName|参数| 格式
            UserName = $"{_validSignParams.DeviceName}&{_validSignParams.ProductKey}",
            Password = "anyPassword123"
        };

        // Act
        var result = signer.VerifyMqttSign(invalidConnectParams, _validProductSecret);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.ClientIdFormatInvalid);
    }

    ///// <summary>
    ///// 场景：认证类型不匹配 → 验证失败
    ///// </summary>
    //[Fact]
    //public void VerifyMqttSign_AuthTypeMismatch_ShouldReturnFailed()
    //{
    //    // Arrange
    //    var signer = new OneProductOneSecretMqttSigner(_validAuthTypePreRegister);
    //    // 生成 免预注册 类型的连接参数
    //    var mismatchSignParams = new MqttSignParams
    //    {
    //        ProductKey = "prod123456",
    //        DeviceName = "device001",
    //        AuthType = _validAuthTypePreRegister,
    //        SignMethod = MqttSignMethod.HmacSha256,
    //        Random = "random123456"
    //    };

    //    // Act
    //    var mismatchConnectParams = signer.GenerateMqttConnectParams(mismatchSignParams, _validProductSecret);
    //    mismatchSignParams.AuthType = MqttAuthType.OneProductOneSecretNoPreRegister;// 被篡改， 与签名器类型不匹配
    //    var result = signer.VerifyMqttSign(mismatchConnectParams, _validProductSecret);

    //    // Assert
    //    result.Succeeded.ShouldBeFalse();
    //    result.Errors.ShouldContain(e => e.Code == IoTMqttErrorCodes.AuthTypeMismatch);


    //}
    #endregion
}
