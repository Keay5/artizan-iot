using Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Tests;

/// <summary>
/// JavaScriptCodecPoolKeyHelper 单元测试
/// 测试覆盖场景：
/// 1. 合法场景：全字段、仅产品Key、产品Key+脚本名、产品Key+版本号
/// 2. 特殊字符场景：产品Key/脚本名/版本号含_pk:/_sn:/_v:/_/:等字符
/// 3. 非法场景：空池键、前缀错误、产品Key为空、字段格式错误等
/// 4. 极端场景：字段值包含完整标识字符串（如_pk:）
/// </summary>
public class JavaScriptCodecPoolKeyHelperTests
{
    #region 常量定义（复用被测类常量，避免硬编码）
    private const string ValidPoolKeyPrefix = "JavaScriptDataCodec_";
    private const string InvalidPoolKeyPrefix = "PythonDataCodec_";
    #endregion

    #region 生成池键测试（GeneratePoolKey）
    [Fact]
    public void GeneratePoolKey_WithAllFields_ShouldReturnValidKey()
    {
        // Arrange
        var productKey = "prod001";
        var scriptName = "scriptA";
        var scriptVersion = "1.0.2";

        // Act
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey, scriptName, scriptVersion);

        // Assert
        poolKey.ShouldNotBeNullOrEmpty();
        poolKey.ShouldStartWith(ValidPoolKeyPrefix);
        poolKey.ShouldContain($"pk:{productKey}");
        poolKey.ShouldContain($"_sn:{scriptName}");
        poolKey.ShouldContain($"_v:{scriptVersion}");
    }

    [Fact]
    public void GeneratePoolKey_OnlyProductKey_ShouldReturnValidKey()
    {
        // Arrange
        var productKey = "prod001";

        // Act
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey);

        // Assert
        poolKey.ShouldBe($"{ValidPoolKeyPrefix}pk:{productKey}");
    }

    [Fact]
    public void GeneratePoolKey_ProductKeyWithSpecialChars_ShouldReturnValidKey()
    {
        // Arrange
        var productKey = "prod_001:test_pk:123"; // 包含_pk:/_/:等特殊字符
        var scriptName = "script:name_sn:v1";    // 包含_sn:/:等特殊字符
        var scriptVersion = "v2.0:beta_v:rc1";  // 包含_v:/:等特殊字符

        // Act
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey, scriptName, scriptVersion);

        // Assert
        poolKey.ShouldStartWith(ValidPoolKeyPrefix);
        poolKey.ShouldContain($"pk:{productKey}");
        poolKey.ShouldContain($"_sn:{scriptName}");
        poolKey.ShouldContain($"_v:{scriptVersion}");
    }

    [Fact]
    public void GeneratePoolKey_ProductKeyIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? productKey = null;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey!));

        // Assert
        exception.ParamName.ShouldBe("productKey");
        exception.Message.ShouldContain("产品Key不能为空");
    }

    [Fact]
    public void GeneratePoolKey_ProductKeyIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var productKey = "   ";

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey));

        // Assert
        exception.ParamName.ShouldBe("productKey");
        exception.Message.ShouldContain("产品Key不能仅包含空白字符");
    }
    #endregion

    #region 解析池键测试（ParsePoolKey）
    [Fact]
    public void ParsePoolKey_WithAllFields_ShouldReturnCorrectValues()
    {
        // Arrange
        var productKey = "prod001";
        var scriptName = "scriptA";
        var scriptVersion = "1.0.2";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey, scriptName, scriptVersion);

        // Act
        var (parsedPk, parsedSn, parsedSv) = JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey);

        // Assert
        parsedPk.ShouldBe(productKey);
        parsedSn.ShouldBe(scriptName);
        parsedSv.ShouldBe(scriptVersion);
    }

    [Fact]
    public void ParsePoolKey_OnlyProductKey_ShouldReturnCorrectValues()
    {
        // Arrange
        var productKey = "prod001";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey);

        // Act
        var (parsedPk, parsedSn, parsedSv) = JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey);

        // Assert
        parsedPk.ShouldBe(productKey);
        parsedSn.ShouldBeNull();
        parsedSv.ShouldBeNull();
    }

    [Fact]
    public void ParsePoolKey_ProductKeyWithSpecialChars_ShouldReturnCorrectValues()
    {
        // Arrange - 极端场景：字段值包含完整标识字符串
        var productKey = "prod_001:test_pk:123"; // 包含_pk:
        var scriptName = "script:name_sn:v1";    // 包含_sn:
        var scriptVersion = "v2.0:beta_v:rc1";  // 包含_v:
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey, scriptName, scriptVersion);

        // Act
        var (parsedPk, parsedSn, parsedSv) = JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey);

        // Assert - 确保特殊字符被完整保留
        parsedPk.ShouldBe(productKey);
        parsedSn.ShouldBe(scriptName);
        parsedSv.ShouldBe(scriptVersion);
    }

    [Fact]
    public void ParsePoolKey_ProductKeyPlusVersion_ShouldReturnCorrectValues()
    {
        // Arrange
        var productKey = "prod001";
        var scriptVersion = "2.0";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey, null, scriptVersion);

        // Act
        var (parsedPk, parsedSn, parsedSv) = JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey);

        // Assert
        parsedPk.ShouldBe(productKey);
        parsedSn.ShouldBeNull();
        parsedSv.ShouldBe(scriptVersion);
    }

    [Fact]
    public void ParsePoolKey_EmptyPoolKey_ShouldThrowArgumentException()
    {
        // Arrange
        var poolKey = string.Empty;

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.ParamName.ShouldBe("poolKey");
        exception.Message.ShouldContain("池键不能为空");
    }

    [Fact]
    public void ParsePoolKey_InvalidPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var poolKey = $"{InvalidPoolKeyPrefix}pk:prod001";

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.Message.ShouldContain($"必须以[{ValidPoolKeyPrefix}]为前缀");
    }

    [Fact]
    public void ParsePoolKey_OnlyPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var poolKey = ValidPoolKeyPrefix;

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.Message.ShouldContain("移除前缀后无有效内容");
    }

    [Fact]
    public void ParsePoolKey_ProductKeyFlagNotAtStart_ShouldThrowArgumentException()
    {
        // Arrange - 产品Key标识不在开头
        var poolKey = $"{ValidPoolKeyPrefix}sn:scriptA_pk:prod001";

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.Message.ShouldContain("核心内容必须以[pk:]开头");
    }

    [Fact]
    public void ParsePoolKey_EmptyProductKey_ShouldThrowArgumentException()
    {
        // Arrange - 产品Key为空
        var poolKey = $"{ValidPoolKeyPrefix}pk:_sn:scriptA";

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.Message.ShouldContain("产品Key不能为空");
    }

    [Fact]
    public void ParsePoolKey_InvalidFieldFormat_ShouldThrowArgumentException()
    {
        // Arrange - 字段格式错误（无冒号分隔）
        var poolKey = $"{ValidPoolKeyPrefix}pkprod001_snscriptA";

        // Act
        var exception = Should.Throw<ArgumentException>(() =>
            JavaScriptCodecPoolKeyHelper.ParsePoolKey(poolKey));

        // Assert
        exception.Message.ShouldContain("核心内容必须以[pk:]开头");
    }
    #endregion

    #region 便捷方法测试（GetXXXFromPoolKey）
    [Fact]
    public void GetProductKeyFromPoolKey_ValidKey_ShouldReturnCorrectValue()
    {
        // Arrange
        var productKey = "prod_001:test_pk:123";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey(productKey);

        // Act
        var parsedPk = JavaScriptCodecPoolKeyHelper.GetProductKeyFromPoolKey(poolKey);

        // Assert
        parsedPk.ShouldBe(productKey);
    }

    [Fact]
    public void GetScriptNameFromPoolKey_WithScriptName_ShouldReturnCorrectValue()
    {
        // Arrange
        var scriptName = "script:name_sn:v1";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("prod001", scriptName);

        // Act
        var parsedSn = JavaScriptCodecPoolKeyHelper.GetScriptNameFromPoolKey(poolKey);

        // Assert
        parsedSn.ShouldBe(scriptName);
    }

    [Fact]
    public void GetScriptNameFromPoolKey_WithoutScriptName_ShouldReturnNull()
    {
        // Arrange
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("prod001");

        // Act
        var parsedSn = JavaScriptCodecPoolKeyHelper.GetScriptNameFromPoolKey(poolKey);

        // Assert
        parsedSn.ShouldBeNull();
    }

    [Fact]
    public void GetScriptVersionFromPoolKey_WithVersion_ShouldReturnCorrectValue()
    {
        // Arrange
        var scriptVersion = "v2.0:beta_v:rc1";
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("prod001", null, scriptVersion);

        // Act
        var parsedSv = JavaScriptCodecPoolKeyHelper.GetScriptVersionFromPoolKey(poolKey);

        // Assert
        parsedSv.ShouldBe(scriptVersion);
    }

    [Fact]
    public void GetScriptVersionFromPoolKey_WithoutVersion_ShouldReturnNull()
    {
        // Arrange
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("prod001");

        // Act
        var parsedSv = JavaScriptCodecPoolKeyHelper.GetScriptVersionFromPoolKey(poolKey);

        // Assert
        parsedSv.ShouldBeNull();
    }
    #endregion

    #region 合法性校验测试（IsValidPoolKey）
    [Fact]
    public void IsValidPoolKey_ValidKey_ShouldReturnTrue()
    {
        // Arrange
        var poolKey = JavaScriptCodecPoolKeyHelper.GeneratePoolKey("prod001");

        // Act
        var isValid = JavaScriptCodecPoolKeyHelper.IsValidPoolKey(poolKey, out var errorMsg);

        // Assert
        isValid.ShouldBeTrue();
        errorMsg.ShouldBeNull();
    }

    [Fact]
    public void IsValidPoolKey_InvalidKey_ShouldReturnFalse()
    {
        // Arrange
        var poolKey = $"{InvalidPoolKeyPrefix}pk:prod001";

        // Act
        var isValid = JavaScriptCodecPoolKeyHelper.IsValidPoolKey(poolKey, out var errorMsg);

        // Assert
        isValid.ShouldBeFalse();
        errorMsg.ShouldNotBeNull();
        errorMsg.ShouldContain($"必须以[{ValidPoolKeyPrefix}]为前缀");
    }
    #endregion
}
