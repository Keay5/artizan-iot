using Shouldly;
using System;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products.MessageParsings;

/// <summary>
/// TSL消息解析器单元测试
/// 覆盖场景：
/// 1. 正常解析流程
/// 2. 异常处理（空参数、无效脚本、类型错误）
/// 3. 不同TSL数据类型的转换
/// </summary>
public abstract class ProductMessageParsing_Tests<TStartupModule> : IoTHubDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    // 测试用解析器
    private readonly JavaScriptTopicMessageParser _parser;
    private readonly string _topic = "/user/update";

    /// <summary>
    /// 测试初始化
    /// </summary>
    public ProductMessageParsing_Tests()
    {

        _parser = GetRequiredService<JavaScriptTopicMessageParser>();
    }

    #region 原始数据转TSL格式测试

    /// <summary>
    /// 测试整数类型原始数据转TSL格式
    /// </summary>
    [Fact]
    public async Task RawDataToProtocolData_Int8Type_ShouldReturnCorrectJson()
    {
        // Arrange
        // 原始数据：0x19（温度25℃）
        var rawData = new byte[] { 0x19 };

        // 规范的解析脚本
        var script = @"
                function rawDataToProtocolData(rawData) {
                    return {
                        id: '123456',
                        version: '1.0',
                        params: {
                            temperature: bytesToTslType(rawData, 'int8')
                        },
                        method: 'thing.event.property.post'
                    };
                }
            ";

        // Act
        var result = await _parser.RawDataToProtocolDataAsync(rawData, script);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"temperature\":25");
        result.ShouldContain("\"method\":\"thing.event.property.post\"");
    }

    /// <summary>
    /// 测试浮点类型原始数据转TSL格式
    /// </summary>
    [Fact]
    public async Task RawDataToProtocolData_FloatType_ShouldReturnCorrectJson()
    {
        // Arrange
        // 12.5f 的大端模式 float 字节（0x41 0x48 0x00 0x00）
        byte[] rawData = new byte[] { 0x41, 0x48, 0x00, 0x00 };

        // 解析脚本：将 rawDataInput 转换为 float 并返回预期结构
        string script = @"
            function rawDataToProtocolData(rawData) {
                // 从字节数组提取前4个字节（float占4字节），大端模式
                var floatBytes = rawData.slice(0, 4);
                var humidity = bytesToTslType(floatBytes, 'float', { endian: 'big' });
                return { params: { humidity: humidity } };
            }
        ";

        // Act
        string? result = await _parser.RawDataToProtocolDataAsync(rawData, script);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("{\"params\":{\"humidity\":12.5}}");
    }

    #endregion

    #region TSL格式转原始数据测试

    /// <summary>
    /// 测试布尔类型TSL数据转原始字节
    /// </summary>
    [Fact]
    public async Task ProtocolDataToRawData_BoolType_ShouldReturnCorrectBytes()
    {
        // Arrange
        var protocolData = @"
            {
                ""params"": {
                    ""switch"": true
                },
                ""method"": ""thing.service.property.set""
            }";

        var script = @"
                function protocolDataToRawData(jsonObj) {
                    return tslTypeToBytes(jsonObj.params.switch, 'bool');
                }
            ";

        // Act
        var result = await _parser.ProtocolDataToRawDataAsync(protocolData, script);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
        result[0].ShouldBe((byte)0x01);
    }

    /// <summary>
    /// 测试字符串类型TSL数据转原始字节
    /// </summary>
    [Fact]
    public async Task ProtocolDataToRawData_StringType_ShouldReturnCorrectBytes()
    {
        // Arrange
        var protocolData = @"
            {
                ""params"": {
                    ""deviceName"": ""test001""
                }
            }";

        var script = @"
                function protocolDataToRawData(jsonObj) {
                    return tslTypeToBytes(jsonObj.params.deviceName, 'string');
                }
            ";

        // Act
        var result = await _parser.ProtocolDataToRawDataAsync(protocolData, script);

        // Assert
        result.ShouldNotBeNull();
        var str = Encoding.UTF8.GetString(result);
        str.ShouldBe("test001");
    }

    #endregion

    #region 异常场景测试

    /// <summary>
    /// 测试空原始数据解析
    /// </summary>
    [Fact]
    public async Task RawDataToProtocolData_NullRawData_ShouldReturnNull()
    {
        // Arrange
        byte[]? rawData = null;
        var script = "function rawDataToProtocolData() { return {}; }";

        // Act
        var result = await _parser.RawDataToProtocolDataAsync(rawData!, script);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试无效脚本解析
    /// </summary>
    [Fact]
    public void RawDataToProtocolData_InvalidScript_ShouldThrowException()
    {
        // Arrange
        var rawData = new byte[] { 0x01 };
        var invalidScript = "invalid javascript code";

        // Act & Assert
        Should.Throw<Exception>(async () => await _parser.RawDataToProtocolDataAsync(rawData, invalidScript));
    }

    /// <summary>
    /// 测试非数组返回值
    /// </summary>
    [Fact]
    public void ProtocolDataToRawData_NonArrayResult_ShouldReturnNull()
    {
        // Arrange
        var protocolData = "{\"params\":{}}";
        var script = @"
                function protocolDataToRawData() {
                    return 'not an array'; // 不符合规范
                }
            ";

        // Act
        var result = _parser.ProtocolDataToRawDataAsync(protocolData, script);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}
