using Artizan.IoT.Mqtts.Messages.Parsers;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics;

/// <summary>
/// Topic模板解析工具（核心工具类，处理占位符→正则转换）
/// 设计理念：
/// 1. 支持双匹配模式：${占位符} + MQTT标准通配符（+/#）；
/// 2. 缓存优化：解析结果缓存，避免重复编译正则，提升高并发性能；
/// 3. 线程安全：无状态设计+ConcurrentDictionary缓存，支持全局复用；
/// 4. 兼容标准：严格遵循MQTT Topic层级规则（/分隔，+匹配单层，#匹配多层）。
/// </summary>
public class MqttTopicTemplateParser : ISingletonDependency
{
    // 匹配${xxx}格式占位符的正则（预编译优化）
    private static readonly Regex _placeholderRegex = new(@"\$\{([a-zA-Z0-9._]+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 解析结果缓存（线程安全字典，Key=模板，Value=（解析结果+缓存时间））
    private readonly ConcurrentDictionary<string, (TopicTemplateParseResult Result, DateTime CacheTime)> _parseCache;

    // 路由系统配置选项
    private readonly MqttRouterOptions _options;

    public MqttTopicTemplateParser(IOptions<MqttRouterOptions> options)
    {
        _options = options.Value;
        _parseCache = new ConcurrentDictionary<string, (TopicTemplateParseResult, DateTime)>();
    }

    /// <summary>
    /// 解析Topic模板，生成匹配正则和占位符列表
    /// 示例1：/sys/${productKey}/${deviceName}/event → 正则：^/sys/(?<productKey>[^/]+)/(?<deviceName>[^/]+)/event$
    /// 示例2：/ota/${productKey}/# → 正则：^/ota/(?<productKey>[^/]+)/.*$
    /// </summary>
    public TopicTemplateParseResult Parse(string template)
    {
        Check.NotNullOrWhiteSpace(template, nameof(template));

        // 启用缓存时，优先从缓存获取
        if (_options.EnableTopicCache && _parseCache.TryGetValue(template, out var cacheItem))
        {
            if (DateTime.UtcNow - cacheItem.CacheTime < _options.CacheExpiration)
            {
                return cacheItem.Result;
            }
            _parseCache.TryRemove(template, out _); // 缓存过期，移除旧缓存
        }

        // 解析占位符和通配符
        var placeholderNames = new List<string>();
        var regexPattern = template;

        // 处理${xxx}占位符→正则命名捕获组
        regexPattern = _placeholderRegex.Replace(regexPattern, match =>
        {
            var placeholderName = match.Groups[1].Value;
            placeholderNames.Add(placeholderName);
            return $"(?<{placeholderName}>[^/]+)"; // 占位符匹配非/字符（Topic层级分隔符）
        });

        // 处理MQTT标准通配符
        regexPattern = ProcessMqttWildcards(regexPattern);

        // 生成完整正则（锚定首尾，确保精确匹配）
        var fullRegex = new Regex($"^{regexPattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var result = new TopicTemplateParseResult(fullRegex, placeholderNames);

        // 存入缓存
        if (_options.EnableTopicCache)
        {
            _parseCache.TryAdd(template, (result, DateTime.UtcNow));
        }

        return result;
    }

    /// <summary>
    /// 清理过期缓存（后台服务定时调用）
    /// </summary>
    public void ClearExpiredCache()
    {
        if (!_options.EnableTopicCache) return;

        var expiredKeys = _parseCache
            .Where(kv => DateTime.UtcNow - kv.Value.CacheTime >= _options.CacheExpiration)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _parseCache.TryRemove(key, out _);
        }
    }

    #region 私有辅助方法：处理MQTT标准通配符
    /// <summary>
    /// + → 匹配单层非/字符；# → 仅允许在末尾，匹配多层任意字符
    /// </summary>
    private string ProcessMqttWildcards(string pattern)
    {
        // 处理多层通配符#（仅允许在末尾）
        if (pattern.EndsWith("/#"))
        {
            pattern = pattern.Replace("/#", "/.*");
        }
        else if (pattern == "#")
        {
            throw new ArgumentException("不允许使用全局通配符#，请指定具体前缀（如/sys/#）");
        }

        // 处理单层通配符+
        pattern = pattern.Replace("+", "[^/]+");

        return pattern;
    }
    #endregion
}
