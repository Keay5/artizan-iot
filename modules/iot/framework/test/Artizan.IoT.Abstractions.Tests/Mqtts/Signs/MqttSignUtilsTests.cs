using Artizan.IoT.Devices;
using Artizan.IoT.Mqtts.Signs;
using Artizan.IoT.Products;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace Artizan.IoT.Abstractions.Tests.Mqtts.Signs;

public class MqttSignUtilsTests
{
    #region 测试基础数据
    // 正则兼容的合法参数（匹配 ProductConsts/DevicesConsts 正则）
    private const string ValidProductKey = "a1B2c3D4e5"; // 字母+数字，符合ProductKeyRegex
    private const string ValidDeviceName = "Device_001-Test@123."; // 字母+数字+合法特殊字符，符合DeviceNameRegex
    private const string InvalidProductKey = "a1B2#3D4e5"; // 包含非法字符#，不匹配ProductKeyRegex
    private const string InvalidDeviceName = "Device_001?Test"; // 包含非法字符?，不匹配DeviceNameRegex

    // 合法ClientId和UserName
    private const string ValidClientId = "AnyPrefix|authType=2,secureMode=2,signMethod=HmacSha2,timestamp=1699999999999,random=abc123,instanceId=iot-xxx|";
    private const string ValidUserName = $"{ValidDeviceName}&{ValidProductKey}"; // 格式：DeviceName&ProductKey
    private const string ValidOneDeviceClientId = "DevicePrefix|authType=1,secureMode=3,signMethod=HmacSha2,timestamp=1699999999999|";
    private const string ValidOneProductNoPreRegClientId = "NoPreRegPrefix|authType=3,secureMode=-2,signMethod=2,timestamp=1699999999999,random=def456|";

    // 非法ClientId/UserName
    private const string InvalidClientId_NoSeparator = "DevicePrefixauthType=1,secureMode=2"; // 无|分隔符
    private const string InvalidClientId_OnlyOnePart = "DevicePrefix|"; // 只有前缀，无参数部分
    private const string InvalidUserName_NoSeparator = $"{ValidDeviceName}{ValidProductKey}"; // 无&分隔符
    private const string InvalidUserName_OnlyDeviceName = $"{ValidDeviceName}&"; // 只有DeviceName，无ProductKey
    private const string EmptyClientId = "";
    private const string NullClientId = null;
    private const string EmptyUserName = "";
    private const string NullUserName = null;

    // 缺少核心参数的ClientId
    private const string ClientId_MissingAuthType = "Prefix|secureMode=2,signMethod=2,timestamp=1699999999999|";
    private const string ClientId_MissingSecureMode = "Prefix|authType=2,signMethod=2,timestamp=1699999999999|";
    private const string ClientId_MissingRandom = "Prefix|authType=2,secureMode=2,signMethod=2,timestamp=1699999999999|"; // 一型一密缺少random
    #endregion

    #region ParseMqttClientIdAndUserName 测试（核心合并解析逻辑）
    [Fact]
    public void ParseMqttClientIdAndUserName_ShouldReturnValidParams_WhenClientIdAndUserNameValid()
    {
        // Arrange
        var clientId = ValidClientId;
        var userName = ValidUserName;

        // Act
        var result = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

        // Assert
        result.ShouldNotBeNull();
        // 从UserName提取的参数
        result.ProductKey.ShouldBe(ValidProductKey);
        result.DeviceName.ShouldBe(ValidDeviceName);
        // 从ClientId解析的参数
        result.AuthType.ShouldBe(MqttAuthType.OneProductOneSecretPreRegister); // authType=2
        result.SecureMode.ShouldBe(2);
        result.SignMethod.ShouldBe(MqttSignMethod.HmacSha256); // signMethod=HmacSha256
        result.Timestamp.ShouldBe("1699999999999");
        result.Random.ShouldBe("abc123");
        result.InstanceId.ShouldBe("iot-xxx");
    }

    [Fact]
    public void ParseMqttClientIdAndUserName_ShouldUseDefaultValue_WhenParamMissing()
    {
        // Arrange：缺少authType和secureMode的ClientId
        var clientId = "Prefix|signMethod=100,timestamp=1699999999999|"; // signMethod=100（非法，取默认）
        var userName = ValidUserName;

        // Act
        var mqttAuthParams = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

        // Assert
        mqttAuthParams.ShouldNotBeNull();
        mqttAuthParams.AuthType.ShouldBe(MqttAuthType.OneDeviceOneSecret); // 默认值
        mqttAuthParams.SecureMode.ShouldBe(MqttSecureModeConstants.Tcp); // 默认值2
        mqttAuthParams.SignMethod.ShouldBe(MqttSignMethod.HmacSha256); // signMethod=3非法，取默认
        mqttAuthParams.ProductKey.ShouldBe(ValidProductKey);
        mqttAuthParams.DeviceName.ShouldBe(ValidDeviceName);
        mqttAuthParams.Timestamp.ShouldBe("1699999999999");
        mqttAuthParams.Random.ShouldBeNull();
        mqttAuthParams.InstanceId.ShouldBeNull();
    }

    [Fact]
    public void ParseMqttClientIdAndUserName_ShouldReturnNull_WhenClientIdInvalid()
    {
        // Arrange：非法ClientId列表
        var testCases = new List<(string ClientId, string UserName)>
        {
            (InvalidClientId_NoSeparator, ValidUserName),
            (InvalidClientId_OnlyOnePart, ValidUserName),
            (EmptyClientId, ValidUserName),
            (NullClientId, ValidUserName),
            (ValidClientId, EmptyUserName), // UserName为空，ProductKey/DeviceName为null，但ClientId合法仍返回对象
            (ValidClientId, NullUserName)
        };

        // Act & Assert
        foreach (var (clientId, userName) in testCases)
        {
            var result = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

            if (clientId == InvalidClientId_NoSeparator || clientId == InvalidClientId_OnlyOnePart || string.IsNullOrWhiteSpace(clientId))
            {
                result.ShouldBeNull($"ClientId: {clientId}, UserName: {userName} 应返回null");
            }
            else
            {
                // UserName为空时，ClientId合法仍返回对象，但ProductKey/DeviceName为null
                result.ShouldNotBeNull();
                result.ProductKey.ShouldBeNull();
                result.DeviceName.ShouldBeNull();
            }
        }
    }

    [Fact]
    public void ParseMqttClientIdAndUserName_ShouldSupportNegativeSecureMode()
    {
        // Arrange：一型一密免预注册（secureMode=-2）
        var clientId = ValidOneProductNoPreRegClientId;
        var userName = ValidUserName;

        // Act
        var result = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

        // Assert
        result.ShouldNotBeNull();
        result.SecureMode.ShouldBe(-2); // 负数解析正常
        result.AuthType.ShouldBe(MqttAuthType.OneProductOneSecretNoPreRegister);
        result.Random.ShouldBe("def456");
    }

    [Fact]
    public void ParseMqttClientIdAndUserName_ShouldSetDefaultAuthType_WhenAuthTypeMissing()
    {
        // Arrange：ClientId缺少authType参数
        var clientId = ClientId_MissingAuthType;
        var userName = ValidUserName;

        // Act
        var result = MqttSignUtils.ParseMqttClientIdAndUserName(clientId, userName);

        // Assert
        result.ShouldNotBeNull();
        result.AuthType.ShouldBe(MqttAuthType.OneDeviceOneSecret); // 重构后默认值
    }
    #endregion

    #region ExtractProductAndDevice 测试（UserName解析）
    [Fact]
    public void ExtractProductAndDevice_ShouldReturnValidValues_WhenUserNameValid()
    {
        // Arrange
        var userName = ValidUserName;

        // Act
        var (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice(userName);

        // Assert
        productKey.ShouldBe(ValidProductKey);
        deviceName.ShouldBe(ValidDeviceName);
    }

    [Fact]
    public void ExtractProductAndDevice_ShouldReturnNull_WhenUserNameInvalid()
    {
        // Act & Assert
        var (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice(InvalidUserName_NoSeparator);
        productKey.ShouldBeNull();
        deviceName.ShouldBeNull();

        (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice(InvalidUserName_OnlyDeviceName);
        productKey.ShouldBeNull();
        deviceName.ShouldBe(ValidDeviceName);

        (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice(EmptyUserName);
        productKey.ShouldBeNull();
        deviceName.ShouldBeNull();

        (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice(NullUserName);
        productKey.ShouldBeNull();
        deviceName.ShouldBeNull();

        (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice("&OnlyProductKey");    // 只有分隔符，无 DeviceName
        productKey.ShouldBe("OnlyProductKey");
        deviceName.ShouldBeNull();

        (productKey, deviceName) = MqttSignUtils.ExtractProductAndDevice("OnlyDeviceName&");    // 只有分隔符，无 ProductKey
        productKey.ShouldBeNull();
        deviceName.ShouldBe("OnlyDeviceName");

    }

    #endregion

    #region IsValidProductKey 测试（依赖ProductConsts.ProductKeyRegex）
    [Fact]
    public void IsValidProductKey_ShouldReturnTrue_WhenProductKeyMatchesRegex()
    {
        // Arrange：符合ProductKeyRegex的合法值（假设正则允许字母、数字、_、-、@、()，长度4-30）
        var validProductKeys = new List<string>
        {
            ValidProductKey,
            "abc123", // 最小长度4
            "a1B2c3D4e5f6g7h8i9j0k1l2m3n430", // 最大长度30
            "a1B2c3D4e5f6g7h8i9j0k1l2m3n43", // 最大长度30
            "a-b_c@(123)" // 合法特殊字符
        };

        // Act & Assert
        foreach (var productKey in validProductKeys)
        {
            var result = MqttSignUtils.IsValidProductKey(productKey);
            result.ShouldBeTrue($"ProductKey: {productKey} 应匹配正则{ProductConsts.ProductKeyRegex}");
        }
    }

    [Fact]
    public void IsValidProductKey_ShouldReturnFalse_WhenProductKeyDoesNotMatchRegex()
    {
        // Arrange：不符合ProductKeyRegex的非法值
        var invalidProductKeys = new List<string>
        {
            InvalidProductKey, // 包含非法字符#
            "abc", // 长度不足4
            "a1B2c3D4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9", // 长度超过30
            string.Empty,
            null,
            "a1B2 3D4e5" // 包含空格（正则不允许）
        };

        // Act & Assert
        foreach (var productKey in invalidProductKeys)
        {
            var result = MqttSignUtils.IsValidProductKey(productKey);
            result.ShouldBeFalse($"ProductKey: {productKey} 不应匹配正则{ProductConsts.ProductKeyRegex}");
        }
    }
    #endregion

    #region IsValidDeviceName 测试（依赖DevicesConsts.DeviceNameRegex）
    [Fact]
    public void IsValidDeviceName_ShouldReturnTrue_WhenDeviceNameMatchesRegex()
    {
        // Arrange：符合DeviceNameRegex的合法值（假设正则允许字母、数字、_、-、@、.、:，长度4-32）
        var validDeviceNames = new List<string>
        {
            ValidDeviceName,
            "dev1", // 最小长度4
            "Device_001-Test@123.456789012345", // 最大长度32
            "a-b_c@.:123" // 合法特殊字符
        };

        // Act & Assert
        foreach (var deviceName in validDeviceNames)
        {
            var result = MqttSignUtils.IsValidDeviceName(deviceName);
            result.ShouldBeTrue($"DeviceName: {deviceName} 应匹配正则{DevicesConsts.DeviceNameRegex}");
        }
    }

    [Fact]
    public void IsValidDeviceName_ShouldReturnFalse_WhenDeviceNameDoesNotMatchRegex()
    {
        // Arrange：不符合DeviceNameRegex的非法值
        var invalidDeviceNames = new List<string>
        {
            InvalidDeviceName, // 包含非法字符?
            "dev", // 长度不足4
            "Device_001-Test@123.456789012345678901234567890123", // 长度超过32
            string.Empty,
            null,
            "Device 001" // 包含空格（正则不允许）
        };

        // Act & Assert
        foreach (var deviceName in invalidDeviceNames)
        {
            var result = MqttSignUtils.IsValidDeviceName(deviceName);
            result.ShouldBeFalse($"DeviceName: {deviceName} 不应匹配正则{DevicesConsts.DeviceNameRegex}");
        }
    }
    #endregion

    #region ValidateCoreParams 测试（核心参数完整性校验）
    [Fact]
    public void ValidateCoreParams_ShouldReturnNull_WhenParamsComplete()
    {
        // Arrange：各认证类型完整参数
        var oneDeviceParams = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneDeviceOneSecret,
            ProductKey = ValidProductKey,
            DeviceName = ValidDeviceName
        };

        var oneProductPreRegParams = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneProductOneSecretPreRegister,
            ProductKey = ValidProductKey,
            DeviceName = ValidDeviceName,
            Random = "abc123"
        };

        var oneProductNoPreRegParams = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneProductOneSecretNoPreRegister,
            ProductKey = ValidProductKey,
            DeviceName = ValidDeviceName,
            Random = "def456"
        };

        // Act & Assert
        MqttSignUtils.ValidateCoreParams(oneDeviceParams).ShouldBeNull();
        MqttSignUtils.ValidateCoreParams(oneProductPreRegParams).ShouldBeNull();
        MqttSignUtils.ValidateCoreParams(oneProductNoPreRegParams).ShouldBeNull();
    }

    [Fact]
    public void ValidateCoreParams_ShouldReturnErrorMsg_WhenParamsIncomplete()
    {
        // Arrange：各类型不完整参数
        var missingProductKey = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneDeviceOneSecret,
            ProductKey = null,
            DeviceName = ValidDeviceName
        };

        var missingDeviceName = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneDeviceOneSecret,
            ProductKey = ValidProductKey,
            DeviceName = string.Empty
        };

        var oneProductMissingRandom = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneProductOneSecretPreRegister,
            ProductKey = ValidProductKey,
            DeviceName = ValidDeviceName,
            Random = null
        };

        var oneProductEmptyRandom = new MqttAuthParams
        {
            AuthType = MqttAuthType.OneProductOneSecretNoPreRegister,
            ProductKey = ValidProductKey,
            DeviceName = ValidDeviceName,
            Random = ""
        };

        // Act & Assert
        MqttSignUtils.ValidateCoreParams(missingProductKey).ShouldBe("ProductKey不能为空");
        MqttSignUtils.ValidateCoreParams(missingDeviceName).ShouldBe("DeviceName不能为空");
        MqttSignUtils.ValidateCoreParams(oneProductMissingRandom).ShouldBe("一型一密认证必须包含随机数（random参数）");
        MqttSignUtils.ValidateCoreParams(oneProductEmptyRandom).ShouldBe("一型一密认证必须包含随机数（random参数）");
    }

    #endregion
}