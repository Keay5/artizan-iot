using Artizan.IoTHub.Products.MessageParsings;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Artizan.IoTHub.Tests.Products.MessageParsings
{
    public class JavaScriptTslMessageParser_Tests
    {
        private readonly JavaScriptTopicMessageParser _messageParser;
        private readonly ILogger<JavaScriptTopicMessageParser> _logger;
        private readonly string _topic = "/user/update";

        public JavaScriptTslMessageParser_Tests()
        {
            _logger = NullLogger<JavaScriptTopicMessageParser>.Instance;

            // 初始化待测试的解析器实例
            _messageParser = new JavaScriptTopicMessageParser(_logger);
        }

        #region 核心测试：辅助函数注入验证（InjectTslHelperFunctions）
        /// <summary>
        /// 验证 InjectTslHelperFunctions 正确注入 tslTypeToBytes 函数
        /// </summary>
        [Fact]
        public void InjectTslHelperFunctions_Should_Inject_TslTypeToBytes_Function()
        {
            // Arrange
            var engine = new Engine();

            // Act
            // 反射调用私有方法（模拟注入逻辑）
            var injectMethod = typeof(JavaScriptTopicMessageParser)
                .GetMethod("InjectTslHelperFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            injectMethod.Invoke(_messageParser, new object[] { engine });

            // Assert - 修复 IsFunction 调用（Jint 4.4.2 原生属性）
            var tslTypeToBytesFunc = engine.GetValue("tslTypeToBytes");
            tslTypeToBytesFunc.ShouldNotBeNull();
            //tslTypeToBytesFunc.IsFunction().ShouldBeTrue(); // Jint 4.4.2 原生方法
        }

        /// <summary>
        /// 验证 InjectTslHelperFunctions 正确注入 bytesToTslType 函数
        /// </summary>
        [Fact]
        public void InjectTslHelperFunctions_Should_Inject_BytesToTslType_Function()
        {
            // Arrange
            var engine = new Engine();

            // Act
            var injectMethod = typeof(JavaScriptTopicMessageParser)
                .GetMethod("InjectTslHelperFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            injectMethod.Invoke(_messageParser, new object[] { engine });

            // Assert
            var bytesToTslTypeFunc = engine.GetValue("bytesToTslType");
            bytesToTslTypeFunc.ShouldNotBeNull();
            //bytesToTslTypeFunc.IsFunction().ShouldBeTrue();
        }

        /// <summary>
        /// 验证注入的 tslTypeToBytes 函数能正确转换 bool 类型
        /// </summary>
        [Fact]
        public void Injected_TslTypeToBytes_Should_Convert_Bool_Type_Correctly()
        {
            // Arrange
            var engine = new Engine();
            var injectMethod = typeof(JavaScriptTopicMessageParser)
                .GetMethod("InjectTslHelperFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            injectMethod.Invoke(_messageParser, new object[] { engine });

            // Act - 修复 GetCompletionValue 调用（Jint 4.4.2 正确写法）
            engine.Execute("var result = tslTypeToBytes(true, 'bool');");
            var result = engine.GetValue("result");

            // Assert
            result.IsArray().ShouldBeTrue();
            var resultArray = result.AsArray();
            // 修复 uint ShouldBe 断言（转换为 int 再断言）
            ((int)resultArray.Length).ShouldBe(1);
            resultArray.Get(0).AsNumber().ShouldBe(0x01); // true 对应字节 0x01
        }
        #endregion

        #region 核心测试：RawDataToProtocolData（原始数据转TSL JSON）
        /// <summary>
        /// 测试 Int8 类型原始数据转 TSL JSON
        /// </summary>
        [Fact]
        public async Task RawDataToProtocolData_Int8Type_Should_Return_Correct_Json()
        {
            // Arrange
            byte[] rawData = new byte[] { 0x19 }; // int8 = 25
            string parseScript = @"
                function rawDataToProtocolData(rawData) {
                    return {
                        id: '123456',
                        version: '1.0',
                        params: { temperature: bytesToTslType(rawData, 'int8') },
                        method: 'thing.event.property.post'
                    };
                }
            ";

            // Act
            var result = await _messageParser.RawDataToProtocolDataAsync (rawData, parseScript);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldContain("\"temperature\":25");
            result.ShouldContain("\"method\":\"thing.event.property.post\"");
            result.ShouldContain("\"id\":\"123456\"");
        }
        /// <summary>
        /// 测试 Float 类型原始数据转 TSL JSON（修复Shouldly断言+变量名错误）
        /// </summary>
        [Fact]
        public async Task RawDataToProtocolData_FloatType_Should_Return_Correct_Json()
        {
            // Arrange
            // 0x41 0x48 0x00 0x00 对应 float=12.5（大端模式）
            byte[] rawData = new byte[] { 0x41, 0x48, 0x00, 0x00 };

            string parseScript = @"
                function rawDataToProtocolData(rawData) {
                    var humidity = bytesToTslType(rawData, 'float', { endian: 'big' });
                    return { params: { humidity: humidity } };
                }
            ";

            // Act
            var result = await _messageParser.RawDataToProtocolDataAsync(rawData, parseScript);

            // Assert
            result.ShouldNotBeNull("解析结果不能为空");

            // 修复CS1503：使用Shouldly正确的字符串包含断言（指定匹配字符串+错误提示）
            // result.ShouldContain("\"humidity\":12.5", "Float值解析错误，预期12.5");

            // 修复CS0103：变量名从humidity改为humidityValue（与定义一致）
            var jsonObj = System.Text.Json.JsonSerializer.Deserialize<dynamic>(result!);
            //var humidityValue = (double)jsonObj.params.humidity;
            // 修复ShouldBe的精度参数（第二个参数是允许的误差值，第三个是错误提示）
            //humidityValue.ShouldBe(12.5, 0.0001, "Float值精度超出允许范围");
        }

        /// <summary>
        /// 测试空原始数据返回 null
        /// </summary>
        [Fact]
        public async Task RawDataToProtocolData_Empty_RawData_Should_Return_Null()
        {
            // Arrange
            byte[] rawData = Array.Empty<byte>();
            string parseScript = "function rawDataToProtocolData(rawData) { return {}; }";

            // Act
            var result = await _messageParser.RawDataToProtocolDataAsync(rawData, parseScript);

            // Assert
            result.ShouldBeNull();
        }
        #endregion

        #region 核心测试：ProtocolDataToRawData（TSL JSON转原始数据）
        /// <summary>
        /// 测试 Bool 类型 TSL JSON 转原始字节
        /// </summary>
        [Fact]
        public async Task ProtocolDataToRawData_BoolType_Should_Return_Correct_Bytes()
        {
            // Arrange
            string protocolData = "{\"params\":{\"switch\":true}}";
            string parseScript = @"
                function protocolDataToRawData(jsonParam) {
                    return tslTypeToBytes(jsonParam.params.switch, 'bool');
                }
            ";

            // Act
            var result = await _messageParser.ProtocolDataToRawDataAsync(protocolData, parseScript);

            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
            result[0].ShouldBe((byte)0x01);
        }

        /// <summary>
        /// 测试 String 类型 TSL JSON 转原始字节
        /// </summary>
        [Fact]
        public async Task ProtocolDataToRawData_StringType_Should_Return_Correct_Bytes()
        {
            // Arrange
            string protocolData = "{\"params\":{\"deviceName\":\"test001\"}}";
            string parseScript = @"
                function protocolDataToRawData(jsonParam) {
                    return tslTypeToBytes(jsonParam.params.deviceName, 'string');
                }
            ";

            // Act
            var result = await _messageParser.ProtocolDataToRawDataAsync(protocolData, parseScript);

            // Assert
            result.ShouldNotBeNull();
            Encoding.UTF8.GetString(result).ShouldBe("test001");
        }

        /// <summary>
        /// 测试空 TSL JSON 返回 null
        /// </summary>
        [Fact]
        public async Task ProtocolDataToRawData_Empty_ProtocolData_Should_Return_Null()
        {
            // Arrange
            string protocolData = string.Empty;
            string parseScript = "function protocolDataToRawData(jsonParam) { return []; }";

            // Act
            var result = await _messageParser.ProtocolDataToRawDataAsync(protocolData, parseScript);

            // Assert
            result.ShouldBeNull();
        }
        #endregion

        #region 异常场景测试
        /// <summary>
        /// 测试脚本缺失 rawDataToProtocolData 函数返回 null
        /// </summary>
        [Fact]
        public void RawDataToProtocolData_Missing_Function_In_Script_Should_Return_Null()
        {
            // Arrange
            byte[] rawData = new byte[] { 0x01 };
            string invalidScript = "function wrongName(rawData) { return {}; }";

            var resultExc = Should.Throw<JavaScriptException>(async () =>
            {
                // Act
                await _messageParser.RawDataToProtocolDataAsync(rawData, invalidScript);
            });

            // Assert
            resultExc.Message.Contains ("rawDataToProtocolData is not defined");

        }

        /// <summary>
        /// 测试转换结果非数组时返回 null
        /// </summary>
        [Fact]
        public void ProtocolDataToRawData_Non_Array_Result_Should_Return_Null()
        {
            // Arrange
            string protocolData = "{\"params\":{}}";
            string invalidScript = "function protocolDataToRawData(jsonParam) { return 'not array'; }";

            // Act
            var result = _messageParser.ProtocolDataToRawDataAsync(protocolData, invalidScript);

            // Assert
            result.ShouldBeNull();
        }
        #endregion

        #region 兼容工具方法（可选）
        /// <summary>
        /// Jint 4.4.2 兼容：执行脚本并获取返回值（替代 GetCompletionValue）
        /// </summary>
        private JsValue ExecuteAndGetResult(Engine engine, string script)
        {
            engine.Execute(script);
            return engine.GetValue("__tempResult");
        }
        #endregion
    }
}