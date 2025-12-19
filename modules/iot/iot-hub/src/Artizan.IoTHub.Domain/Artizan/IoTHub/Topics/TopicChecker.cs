using Artizan.IoT.Mqtts.Topics;
using Artizan.IoT.Topics;
using System;
using System.Text.RegularExpressions;

namespace Artizan.IoTHub.Topics;

public class TopicChecker
{
    /// <summary>
    /// 判断是否为自定义主题且需要转换Payload
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public static bool IsCustomTopicAndNeedTransformPayload(string topic)
    {
        // TODO: 新增条件判断：从自定义Topic库中是否包含该主题？
        return TopicUtils.EndsWithSnDefaultMarkerIgnoreCase(topic);
    }

    /// <summary>
    /// 判断给定的topic是否匹配物模型【透传】上报设备属性请求Topic：/sys/${productKey}/${deviceName}/thing/model/up_raw
    /// <param name="topic">需要判断的topic字符串</param>
    /// <returns>如果匹配返回true，否则返回false</returns>
    public static bool IsThingModelThroughUpRaw(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return false;
        }

        // 获取主题格式常量
        string patternTemplate = MqttTopicSpeciesConsts.ThingModelCommunication.PassThrough.UpRaw;

        // 将模板中的变量替换为正则表达式（匹配非/的任意字符）
        string regexPattern = Regex.Escape(patternTemplate)
            .Replace(@"\$\{productKey\}", @"([^/]+)")
            .Replace(@"\$\{deviceName\}", @"([^/]+)");

        // 匹配整个字符串
        regexPattern = "^" + regexPattern + "$";

        // 执行正则匹配
        return Regex.IsMatch(topic, regexPattern);
    }

    /// <summary>
    /// 判断给定的topic是否匹配物模型【透传】设置设备属性请求Topic：/sys/${productKey}/${deviceName}/thing/model/down_raw
    /// </summary>
    /// <param name="topic">需要判断的topic字符串</param>
    /// <returns>如果匹配返回true，否则返回false</returns>
    public static bool IsThingModelThroughDownRaw(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return false;
        }

        // 获取主题格式常量
        string patternTemplate = MqttTopicSpeciesConsts.ThingModelCommunication.PassThrough.DownRaw;

        // 将模板中的${productKey}和${deviceName}占位符替换为正则表达式（匹配非/的任意字符）
        string regexPattern = Regex.Escape(patternTemplate)
            .Replace(@"\$\{productKey\}", @"([^/]+)")
            .Replace(@"\$\{deviceName\}", @"([^/]+)");

        // 确保整个字符串完全匹配
        regexPattern = "^" + regexPattern + "$";

        // 执行正则匹配
        return Regex.IsMatch(topic, regexPattern);
    }
}
