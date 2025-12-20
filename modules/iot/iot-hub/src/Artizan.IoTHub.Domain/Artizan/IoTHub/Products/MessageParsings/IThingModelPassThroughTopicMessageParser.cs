using System.Threading.Tasks;

namespace Artizan.IoTHub.Products.MessageParsings;

/// <summary>
/// 什么是消息解析？：
/// https://help.aliyun.com/zh/iot/user-guide/message-parsing?spm=a2c4g.11186623.help-menu-30520.d_2_2_2_2_0.783e27013RVOyJ
/// 
/// 1.自定义Topic消息解析：
///     设备通过自定义Topic发布消息，且Topic携带解析标记（?_sn=default）
///     
/// 2.物模型透传Topic消息解析：
///     数据格式为透传/自定义的产品下的设备与云端进行物模型数据通信时，需要物联网平台调用您提交的消息解析脚本，
///     将上、下行物模型消息数据分别解析为物联网平台定义的标准格式（Alink JSON）和设备的自定义数据格式。
///     
///     查看：https://help.aliyun.com/zh/iot/user-guide/device-properties-events-and-services?spm=a2c4g.11186623.0.0.511e2701m5oJXC#concept-mvc-4tw-y2b
///     中的数据格式为【透传/自定义】：
///         数据格式（上行）：
///             请求Topic：/sys/${productKey}/${deviceName}/thing/model/up_raw
///             响应Topic：/sys/${productKey}/${deviceName}/thing/model/up_raw_reply
///         数据格式（下行）：
///             请求Topic：/sys/${productKey}/${deviceName}/thing/model/down_raw
///             响应Topic：/sys/${productKey}/${ deviceName}/thing/model/down_raw_reply
/// ---
/// https://help.aliyun.com/zh/iot/user-guide/message-parsing?spm=a2c4g.11186623.0.0.164d37577URYqA#concept-rhj-535-42b
/// https://help.aliyun.com/zh/iot/user-guide/sample-javascript-script-1?spm=a2c4g.11186623.0.0.6f0268e0s1gCF1#concept-2384946
/// https://help.aliyun.com/zh/iot/user-guide/submit-a-script-to-parse-tsl-data?spm=a2c4g.11186623.0.0.63c169d2Ly6KIH#concept-185365
/// 定义设备原始数据与平台Alink/TSL格式的双向转换契约
/// </summary>
public interface IThingModelPassThroughTopicMessageParser
{
    /// <summary>
    /// 设备上报原始字节 → TSL/自定义JSON（适配transformPayload脚本）
    /// </summary>
    /// <param name="rawData">原始字节数组</param>
    /// <param name="script">解析脚本,要求脚本中必须实现 function protocolDataToRawData(jsonObj) 函数</param>
    /// <returns>包含topic字段的JSON字符串，解析失败返回null</returns>
    Task<string?> RawDataToProtocolDataAsync(byte[] rawData, string script);

    /// <summary>
    /// TSL/自定义JSON → 设备原始字节（平台下发场景，返回结果需关联topic）
    /// </summary>
    /// <param name="protocolJsonData">包含topic字段的JSON字符串</param>
    /// <param name="script">解析脚本.要求脚本中必须实现 function rawDataToProtocolData(rawData) 函数</param>
    /// <returns>设备可识别的原始字节数组，解析失败返回null</returns>
    Task<byte[]?> ProtocolDataToRawDataAsync(string protocolJsonData, string script);
}