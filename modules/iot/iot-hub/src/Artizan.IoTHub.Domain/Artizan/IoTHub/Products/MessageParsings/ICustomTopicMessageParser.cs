using System.Threading.Tasks;

namespace Artizan.IoTHub.Products.MessageParsings;

/// <summary>
/// 自定义Topic消息解析
/// 资料：
/// ·什么是 自定义Topic消息解析：https://help.aliyun.com/zh/iot/user-guide/parse-messages-that-are-sent-to-custom-topics/?spm=a2c4g.11186623.help-menu-30520.d_2_2_2_2_1.7e112701NwyjjE
/// ·JavaScript脚本示例：https://help.aliyun.com/zh/iot/user-guide/sample-javascript-script-1?spm=a2c4g.11186623.help-menu-30520.d_2_2_2_2_1_1.38b82701eaYY0x
/// </summary>
public interface ICustomTopicMessageParser
{
    /// <summary>
    /// 将设备自定义Topic消息数据转换为JSON格式数据，设备上报数据到物联网平台时调用
    /// </summary>
    /// <param name="topic">
    /// 设备上报/下发消息的Topic
    /// <param name="topic">设备上报完整Topic（），包含解析标记_sn（sn:Script Name的简写），表示需要进行消息
    /// 上报，如：${productKey}/${deviceName}/user/get_status?_sn=default
    /// </param>
    /// <param name="rawData">设备上报原始消息字节流</param>
    /// <param name="script">解析脚本</param>
    /// <returns>Json 对象</returns>
    Task<string?> TransformPayloadAsync(string topic, byte[] rawData, string script);
}
