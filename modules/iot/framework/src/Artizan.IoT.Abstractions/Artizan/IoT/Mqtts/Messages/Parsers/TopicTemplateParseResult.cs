using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

/// <summary>
/// Topic模板解析结果（封装正则+占位符列表）
/// 设计理念：缓存解析结果，避免重复编译正则，提升路由匹配性能
/// </summary>
public record TopicTemplateParseResult(
    Regex TemplateRegex,    // 匹配实际Topic的正则表达式
    List<string> PlaceholderNames // 提取的占位符名称列表
);
