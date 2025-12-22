using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Products.MessageParsings;

/// <summary>
/// 基于JavaScript的Alink JSON数据解析器
/// 
/// 自定义主题消息解析：
/// https://help.aliyun.com/zh/iot/user-guide/submit-a-message-parsing-script?spm=a2c4g.11186623.help-menu-30520.d_2_2_2_2_1_0.73e83a8e2bHuhT&scm=20140722.H_149963._.OR_help-T_cn~zh-V_1
/// 
/// 物模型(透传)消息解析脚本:
/// https://help.aliyun.com/zh/iot/user-guide/submit-a-script-to-parse-tsl-data?spm=a2c4g.11186623.help-menu-30520.d_2_2_2_2_2_0.51542701WhFors&scm=20140722.H_114621._.OR_help-T_cn~zh-V_1
/// https://help.aliyun.com/zh/iot/user-guide/submit-a-script-to-parse-tsl-data?spm=a2c4g.11186623.0.0.63c169d2Ly6KIH#concept-185365
/// 
/// JavaScript脚本示例:
/// https://help.aliyun.com/zh/iot/user-guide/sample-javascript-script?spm=a2c4g.11186623.0.0.6058390bpNe5Rj#concept-2371163
/// </summary>
public class JavaScriptTopicMessageParser : IThingModelPassThroughTopicMessageParser, ICustomTopicMessageParser, ISingletonDependency
{
    private readonly ILogger<JavaScriptTopicMessageParser> _logger;

    public JavaScriptTopicMessageParser(ILogger<JavaScriptTopicMessageParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 将设备原始字节数据转换为 Alink JSON格式 
    /// </summary>
    /// <param name="rawData">设备上报的原始字节数组</param>
    /// <param name="script">解析脚本</param>
    /// <returns> Alink JSON字符串，失败返回null</returns>
    public Task<string?> RawDataToProtocolDataAsync(byte[] rawData, string script)
    {
        if (rawData == null || rawData.Length == 0)
        {
            _logger.LogWarning("原始数据为空，无法转换为 Alink Json 格式");
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            _logger.LogError("解析脚本为空，无法执行 Alink Json 格式转换");
            return Task.FromResult<string?>(null);
        }

        try
        {
            _logger.LogDebug($"开始执行原始数据转 Alink Json 格式解析，原始数据长度：{rawData.Length}字节");

            var engine = new Engine();
            InjectTslHelperFunctions(engine);

            var jsRawDataArray = ConvertToJsArray(engine, rawData);
            engine.SetValue("rawData", jsRawDataArray);
            engine.Execute(script);

            // 执行脚本中的解析函数并序列化结果
            engine.Execute(@"
                    var parseResult = rawDataToProtocolData(rawData);
                    var jsonResult = JSON.stringify(parseResult);
                ");

            var jsonResultValue = engine.GetValue("jsonResult");
            if (jsonResultValue.IsNull() || jsonResultValue.IsUndefined() || jsonResultValue.ToString() == "undefined")
            {
                _logger.LogWarning(" Alink 解析函数返回空结果");
                return null;
            }

            var jsonResult = jsonResultValue.ToString();
            _logger.LogDebug($"原始数据转 Alink Json 格式完成，结果：{jsonResult}");

            return Task.FromResult(jsonResult) ;
        }
        catch (JavaScriptException jsEx)
        {
            _logger.LogError(jsEx, "JavaScript执行异常：{Message}", jsEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "原始数据转 Alink Json 格式失败");
            throw;
        }
    }

    /// <summary>
    /// 将 Alink JSON格式转换为设备原始字节数据,
    /// </summary>
    /// <param name="protocolJsonData"> Alink JSON字符串</param>
    /// <param name="script">解析脚本</param>
    /// <returns>设备原始字节数组，失败返回null</returns>
    public Task<byte[]?> ProtocolDataToRawDataAsync(string protocolJsonData, string script)
    {
        if (string.IsNullOrWhiteSpace(protocolJsonData))
        {
            _logger.LogWarning(" Alink 协议数据为空，无法转换为原始字节");
            return Task.FromResult<byte[]?>(null);
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            _logger.LogError("解析脚本为空，无法执行原始数据转换");
            return Task.FromResult<byte[]?>(null);
        }

        try
        {
            _logger.LogDebug($"开始执行 Alink Json 格式转原始数据解析， Alink 数据：{protocolJsonData}");

            var engine = new Engine();
            InjectTslHelperFunctions(engine);

            engine.SetValue("protocolData", protocolJsonData);
            engine.Execute("var jsonParam = JSON.parse(protocolData);");
            var jsonObj = engine.GetValue("jsonParam");

            engine.Execute(script);
            var resultValue = engine.Invoke("protocolDataToRawData", jsonObj);

            if (!resultValue.IsArray())
            {
                _logger.LogError(" Alink 转换函数返回的不是数组类型，不符合阿里云规范");
                return null;
            }

            var rawData = ConvertToCSharpByteArray(engine, resultValue.AsArray());
            _logger.LogDebug($" Alink Json 格式转原始数据完成，字节长度：{rawData.Length}，十六进制：{BitConverter.ToString(rawData).Replace("-", " ")}");

            return Task.FromResult<byte[]?>(rawData); ;
        }
        catch (JavaScriptException jsEx)
        {
            _logger.LogError(jsEx, "JavaScript执行异常：{Message}", jsEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Alink Json 格式转原始数据失败");
            throw;
        }
    }

    /// <summary>
    /// 将设备自定义Topic消息数据转换为JSON格式数据，设备上报数据到物联网平台时调用
    /// 要求脚本中必须实现 function transformPayload(topic, rawData)函数 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public Task<string?> TransformPayloadAsync(string topic, byte[] rawData, string script)
    {
        if (rawData == null || rawData.Length == 0)
        {
            _logger.LogWarning("原始数据为空，无法转换为Alink格式");
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            _logger.LogError("主题Topic为空，无法执行Alink转换");
            return Task.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            _logger.LogError("解析脚本为空，无法执行Alink转换");
            return Task.FromResult<string?>(null);
        }

        try
        {
            _logger.LogDebug($"开始执行原始数据转Alink格式解析，原始数据长度：{rawData.Length}字节");

            var engine = new Engine();
            InjectTslHelperFunctions(engine);

            var jsRawDataArray = ConvertToJsArray(engine, rawData);
            engine.SetValue("rawData", jsRawDataArray);
            engine.Execute(script);

            // 执行脚本中的解析函数并序列化结果
            engine.Execute(@"
                    var parseResult = transformPayload(topic, rawData);
                    var jsonResult = JSON.stringify(parseResult);
                ");

            var jsonResultValue = engine.GetValue("jsonResult");
            if (jsonResultValue.IsNull() || jsonResultValue.IsUndefined() || jsonResultValue.ToString() == "undefined")
            {
                _logger.LogWarning("transformPayload 解析函数返回空结果");
                return Task.FromResult<string?>(null);
            }

            var jsonResult = jsonResultValue.ToString();
            _logger.LogDebug($"原始数据转Alin格式完成，结果：{jsonResult}");

            return Task.FromResult<string?>(jsonResult);
        }
        catch (JavaScriptException jsEx)
        {
            _logger.LogError(jsEx, "JavaScript执行 transformPayload(topic, rawData) 异常：{Message}", jsEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "原始数据转Alin格式失败");
            throw;
        }
    }

    #region 辅助方法
    /// <summary>
    /// 注入 Alink 类型转换辅助函数到JavaScript引擎
    /// 实现阿里云规范中定义的tslTypeToBytes和bytesToTslType函数
    /// </summary>
    private void InjectTslHelperFunctions(Engine engine)
    {
        var helperScript = @"
                function tslTypeToBytes(value, type, options) {
                    options = options || { endian: 'big' };
                    var bytes = [];
                    
                    switch(type) {
                        case 'int8':
                            bytes.push(value & 0xFF);
                            break;
                        case 'int16':
                            if (options.endian === 'big') {
                                bytes.push((value >> 8) & 0xFF);
                                bytes.push(value & 0xFF);
                            } else {
                                bytes.push(value & 0xFF);
                                bytes.push((value >> 8) & 0xFF);
                            }
                            break;
                        case 'int32':
                            if (options.endian === 'big') {
                                bytes.push((value >> 24) & 0xFF);
                                bytes.push((value >> 16) & 0xFF);
                                bytes.push((value >> 8) & 0xFF);
                                bytes.push(value & 0xFF);
                            } else {
                                bytes.push(value & 0xFF);
                                bytes.push((value >> 8) & 0xFF);
                                bytes.push((value >> 16) & 0xFF);
                                bytes.push((value >> 24) & 0xFF);
                            }
                            break;
                        case 'float':
                            var buffer = new ArrayBuffer(4);
                            var view = new DataView(buffer);
                            view.setFloat32(0, value, options.endian === 'little');
                            bytes = Array.from(new Uint8Array(buffer));
                            break;
                        case 'double':
                            var buffer = new ArrayBuffer(8);
                            var view = new DataView(buffer);
                            view.setFloat64(0, value, options.endian === 'little');
                            bytes = Array.from(new Uint8Array(buffer));
                            break;
                        case 'bool':
                            bytes.push(value ? 0x01 : 0x00);
                            break;
                        case 'string':
                            for (var i = 0; i < value.length; i++) {
                                var charCode = value.charCodeAt(i);
                                if (charCode < 0x80) {
                                    bytes.push(charCode);
                                } else if (charCode < 0x800) {
                                    bytes.push(0xC0 | (charCode >> 6));
                                    bytes.push(0x80 | (charCode & 0x3F));
                                } else if (charCode < 0x10000) {
                                    bytes.push(0xE0 | (charCode >> 12));
                                    bytes.push(0x80 | ((charCode >> 6) & 0x3F));
                                    bytes.push(0x80 | (charCode & 0x3F));
                                }
                            }
                            break;
                        case 'enum':
                            bytes.push(value & 0xFF);
                            break;
                        default:
                            throw new Error('不支持的 Alink 数据类型: ' + type);
                    }
                    return bytes;
                }

                function bytesToTslType(bytes, type, options) {
                    options = options || { endian: 'big' };
                    var value = 0;
                    
                    switch(type) {
                        case 'int8':
                            value = bytes[0] < 0x80 ? bytes[0] : bytes[0] - 0x100;
                            break;
                        case 'int16':
                            if (options.endian === 'big') {
                                value = (bytes[0] << 8) | bytes[1];
                            } else {
                                value = (bytes[1] << 8) | bytes[0];
                            }
                            value = value < 0x8000 ? value : value - 0x10000;
                            break;
                        case 'int32':
                            if (options.endian === 'big') {
                                value = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
                            } else {
                                value = (bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0];
                            }
                            break;
                        case 'float':
                            var buffer = new ArrayBuffer(4);
                            var view = new DataView(buffer);
                            for (var i = 0; i < 4; i++) {
                                view.setUint8(i, bytes[options.endian === 'little' ? 3 - i : i]);
                            }
                            value = view.getFloat32(0);
                            break;
                        case 'double':
                            var buffer = new ArrayBuffer(8);
                            var view = new DataView(buffer);
                            for (var i = 0; i < 8; i++) {
                                view.setUint8(i, bytes[options.endian === 'little' ? 7 - i : i]);
                            }
                            value = view.getFloat64(0);
                            break;
                        case 'bool':
                            value = bytes[0] !== 0;
                            break;
                        case 'string':
                            var str = '';
                            var i = 0;
                            while (i < bytes.length) {
                                if (bytes[i] < 0x80) {
                                    str += String.fromCharCode(bytes[i]);
                                    i++;
                                } else if (bytes[i] < 0xE0) {
                                    str += String.fromCharCode(((bytes[i] & 0x3F) << 6) | (bytes[i+1] & 0x3F));
                                    i += 2;
                                } else {
                                    str += String.fromCharCode(((bytes[i] & 0x1F) << 12) | ((bytes[i+1] & 0x3F) << 6) | (bytes[i+2] & 0x3F));
                                    i += 3;
                                }
                            }
                            value = str;
                            break;
                        case 'enum':
                            value = bytes[0];
                            break;
                        default:
                            throw new Error('不支持的 Alink 数据类型: ' + type);
                    }
                    return value;
                }
            ";

        engine.Execute(helperScript);
        _logger.LogDebug(" Alink 类型转换辅助函数注入完成");
    }

    /// <summary>
    /// 将C#字节数组转换为JavaScript数组
    /// </summary>
    private JsValue ConvertToJsArray(Engine engine, byte[] bytes)
    {
        engine.Execute("var tempArray = [];");
        var jsArray = engine.GetValue("tempArray");

        for (int i = 0; i < bytes.Length; i++)
        {
            int byteValue = bytes[i];
            engine.Execute($"tempArray[{i}] = {byteValue};");
        }

        return jsArray;
    }

    /// <summary>
    /// 将JavaScript数组转换为C#字节数组
    /// </summary>
    private byte[] ConvertToCSharpByteArray(Engine engine, ArrayInstance jsArray)
    {
        var byteList = new List<byte>();

        engine.SetValue("tempJsArray", jsArray);
        engine.Execute("var arrayLength = tempJsArray.length;");
        var lengthObj = engine.GetValue("arrayLength");

        if (!lengthObj.IsNumber())
        {
            _logger.LogWarning("JS数组length属性不是数字类型");
            return byteList.ToArray();
        }

        int arrayLength = Convert.ToInt32(lengthObj.AsNumber());

        for (int i = 0; i < arrayLength; i++)
        {
            engine.Execute($"var arrayItem = tempJsArray[{i}];");
            var item = engine.GetValue("arrayItem");

            if (!item.IsNumber())
            {
                _logger.LogWarning($"JS数组索引{i}不是数字类型，跳过");
                continue;
            }

            var numValue = item.AsNumber();
            if (numValue < 0 || numValue > 255)
            {
                _logger.LogWarning($"JS数组索引{i}的值{numValue}超出字节范围(0-255)，跳过");
                continue;
            }

            byteList.Add(Convert.ToByte(numValue));
        }

        // 清理临时变量
        engine.Execute("delete tempJsArray; delete arrayLength; delete arrayItem;");

        return byteList.ToArray();
    }

    #endregion
}
