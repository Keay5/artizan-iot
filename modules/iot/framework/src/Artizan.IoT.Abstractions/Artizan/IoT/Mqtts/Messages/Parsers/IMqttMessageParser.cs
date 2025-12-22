using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

/// <summary>
/// 消息解析器接口
/// </summary>
public interface IMqttMessageParser
{
    /// <summary>
    /// 支持的解析类型
    /// </summary>
    MqttMessageDataParseType SupportType { get; }

    /// <summary>
    /// 是否匹配当前产品/Topic（用于动态选择解析器）
    /// </summary>
    bool Match(MqttMessageContext context);

    /// <summary>
    /// 执行解析
    /// </summary>
    Task<object> ParseAsync(MqttMessageContext context);
}
