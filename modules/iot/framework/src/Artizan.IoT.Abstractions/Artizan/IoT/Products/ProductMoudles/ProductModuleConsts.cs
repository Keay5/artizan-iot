using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.ProductMoudles;

public static class ProductModuleConsts
{
    /// <summary>
    /// 默认模块的Identifier/TSL functionBlockId（不区分大小写）
    /// </summary>
    public const string DefaultModuleIdentifier = "Default";

    public const string NameCharRegexPattern = @"^[\u4e00-\u9fa5a-zA-Z0-9_]+$";
    public static int MaxNameLength { get; set; } = 30;
    public static int MinNameLength { get; set; } = 4;

    /// <summary>
    /// 系统保留标识符（不区分大小写）
    /// </summary>
    public static readonly HashSet<string> SystemReservedIdentifiers = new HashSet<string>
    {
        "set", "get", "post", "property", "event", "time", "value"
    };
    /// <summary>
    /// 产品模块标识符需满足：
    ///     ♦ 非空
    ///     ♦长度：不超过30个字符，
    ///     ♦允许字符：只允许字母、数字、下划线（_）的组合
    ///     
    ///产品名称正则表达式常量：@"^[a-zA-Z0-9_-]*$"
    ///说明：
    ///1) ^: 表示匹配字符串的开始位置。其与$: 匹配字符串的结束位置，两者结合表示：整个字符串必须完全符合括号内的规则（不能只匹配部分内容）。
    ///2) [a-zA-Z0-9_-]:[]是正则的字符类，表示“匹配方括号内的任意一个字符”。里面的内容是允许出现在产品名称中的所有字符。
    ///                 这是一个字符类（character class），表示匹配方括号内的任意一个字符。
    ///     ♦ a-z : 匹配任意小写字母（从a到z）。
    ///     ♦ A-Z : 匹配任意大写字母（从A到Z）。
    ///     ♦ 0-9 : 匹配任意数字（从0到9）。
    ///     ♦ _ : 匹配下划线。
    /// 3) * : 表示前面的字符类可以出现0次或多次。也就是说，这个字符串可以是空字符串，也可以由上述字符组成。
    /// 4) $ : 表示匹配字符串的结束位置。
    /// </summary>
    public const string IdentifierCharRegexPattern = @"^[a-zA-Z0-9_]+$";
    public static int MaxIdentifierLength { get; set; } = 30;

    public static int MaxVersionLength { get; set; } = 64;
    public static int MaxDescriptionLength { get; set; } = 128;
}
