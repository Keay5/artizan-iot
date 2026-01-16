using System.Text.RegularExpressions;

namespace Artizan.IoT.Products;

public static class ProductConsts
{
    /// <summary>
    /// 产品Key正则表达式（4-30字符，支持中文/字母/数字/_/-/@/()）
    /// </summary>
    public const string ProductKeyRegexPattern = @"^[\u4e00-\u9fa5a-zA-Z0-9_\-@()]{4,30}$";
    /// <summary>
    /// 产品Key正则验证实例
    /// 参数： RegexOptions.Compiled:（预编译，重复使用高效
    /// </summary>
    public static readonly Regex ProductKeyRegex = new Regex(ProductKeyRegexPattern, RegexOptions.Compiled);
}
