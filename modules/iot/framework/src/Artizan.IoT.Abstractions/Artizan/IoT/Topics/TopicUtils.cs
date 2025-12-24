using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Topics;

public static class TopicUtils
{
    #region ?_sn=default
    /*---------------------------------------------------------------------------------------------------------------------
     ?_sn=default 需求背景：
        https://help.aliyun.com/zh/iot/user-guide/submit-a-message-parsing-script?spm=a2c4g.11186623.0.0.4afa68e04kSqKJ#task-2382526

     ---------------------------------------------------------------------------------------------------------------------*/

    // 固定解析标记（原始小写）
    private const string SnDefaultMarker = "?_sn=default";
    // 标记长度（避免重复计算）
    private static readonly int SnDefaultMarkerLength = SnDefaultMarker.Length;

    /// <summary>
    /// 核心方法：判断 Topic 结尾是否包含 ?_sn=default 标记（忽略大小写，精确匹配结尾）
    /// 支持以下结尾格式均判定为有效：
    ///  ?_sn=default（标准小写）
    ///  ?_SN=DEFAULT（全大写）
    ///  ?_Sn=Default（大小写混合）
    ///  ?_sN=DeFaUlT（任意大小写组合）?_sn=default（标准小写）
    ///  ?_SN=DEFAULT（全大写）
    ///  ?_Sn=Default（大小写混合）
    ///  ?_sN=DeFaUlT（任意大小写组合）
    /// </summary>
    /// <param name="topic">待判断的 Topic 字符串（可为 null/空）</param>
    /// <returns>true：Topic 非空且结尾匹配标记（大小写无关）；false：不匹配或 Topic 为空</returns>
    public static bool EndsWithSnDefaultMarkerIgnoreCase(string topic)
    {
        // 空值校验：null 或空白字符串直接返回 false
        if (string.IsNullOrWhiteSpace(topic))
        {
            return false;
        }

        // Topic 长度必须大于等于标记长度，否则不可能包含标记
        if (topic.Length < SnDefaultMarkerLength)
        {
            return false;
        }

        // 截取 Topic 末尾与标记等长的字符串，忽略大小写比对
        string topicSuffix = topic.Substring(topic.Length - SnDefaultMarkerLength);
        return string.Equals(topicSuffix, SnDefaultMarker, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 辅助方法：为 Topic 结尾添加标准 ?_sn=default 标记（已存在则不重复添加，忽略大小写判断）
    /// </summary>
    /// <param name="topic">原始 Topic 字符串</param>
    /// <returns>结尾携带标准标记的 Topic</returns>
    /// <exception cref="ArgumentNullException">Topic 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">Topic 为空白字符串时抛出</exception>
    public static string AddSnDefaultMarkerToEnd(string topic)
    {
        if (topic == null)
        {
            throw new ArgumentNullException(nameof(topic), "Topic 不能为 null");
        }

        string trimmedTopic = topic.Trim();
        if (string.IsNullOrEmpty(trimmedTopic))
        {
            throw new ArgumentException("Topic 不能为空白字符串", nameof(topic));
        }

        // 已包含标记（忽略大小写），直接返回原 Topic
        if (EndsWithSnDefaultMarkerIgnoreCase(trimmedTopic))
        {
            return trimmedTopic;
        }

        // 结尾添加标准小写标记
        return $"{trimmedTopic}{SnDefaultMarker}";
    }

    /// <summary>
    /// 辅助方法：移除 Topic 结尾的 ?_sn=default 标记（忽略大小写，仅匹配结尾时移除）
    /// </summary>
    /// <param name="topic">携带标记的 Topic 字符串</param>
    /// <returns>移除标记后的 Topic（null/空输入直接返回原值）</returns>
    public static string RemoveSnDefaultMarkerFromEnd(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return topic;
        }

        // 仅当结尾匹配标记（忽略大小写）时，移除标记
        if (EndsWithSnDefaultMarkerIgnoreCase(topic))
        {
            return topic.Substring(0, topic.Length - SnDefaultMarkerLength);
        }

        return topic;
    } 
  
    #endregion
}
