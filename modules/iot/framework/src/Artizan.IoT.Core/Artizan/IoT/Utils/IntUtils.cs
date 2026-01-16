using System.Collections.Generic;

namespace Artizan.IoT.Utils;

public class IntUtils
{
    /// <summary>
    /// 解析整数类型参数（支持负数）
    /// </summary>
    /// <param name="paramDict">参数字典</param>
    /// <param name="key">参数键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="minValue">最小值（可选，限制参数范围）</param>
    /// <param name="maxValue">最大值（可选，限制参数范围）</param>
    /// /// <example>
    /// 示例1：解析存在且有效的整数参数
    /// <code>
    /// var paramDict = new Dictionary&lt;string, string&gt; { { "timeout", "30" } };
    /// int timeout = ParseIntParam(paramDict, "timeout", 10); // 返回 30
    /// </code>
    /// 
    /// 示例2：解析存在但无效的参数（非数字），返回默认值
    /// <code>
    /// var paramDict = new Dictionary&lt;string, string&gt; { { "retryCount", "abc" } };
    /// int retryCount = ParseIntParam(paramDict, "retryCount", 3); // 返回 3
    /// </code>
    /// 
    /// 示例3：解析不存在的参数，返回默认值
    /// <code>
    /// var paramDict = new Dictionary&lt;string, string&gt;();
    /// int delay = ParseIntParam(paramDict, "delay", -5); // 返回 -5
    /// </code>
    /// 
    /// 示例4：解析负数参数（支持负数）
    /// <code>
    /// var paramDict = new Dictionary&lt;string, string&gt; { { "offset", "-10" } };
    /// int offset = ParseIntParam(paramDict, "offset", 0); // 返回 -10
    /// </code>
    /// 
    /// 示例5：解析空白值参数，返回默认值
    /// <code>
    /// var paramDict = new Dictionary&lt;string, string&gt; { { "limit", "   " } };
    /// int limit = ParseIntParam(paramDict, "limit", 100); // 返回 100
    /// </code>
    /// </example>
    public static int ParseIntParam(
        IDictionary<string, string> paramDict,
        string key,
        int defaultValue,
        int? minValue = null,
        int? maxValue = null)
    {
        if (paramDict.TryGetValue(key, out var valueStr) &&
            !string.IsNullOrWhiteSpace(valueStr) && // 新增：过滤空白字符串
            int.TryParse(valueStr, out int result))
        {
            // 新增：参数范围校验
            if (minValue.HasValue && result < minValue.Value)
            {
                return defaultValue;
            }
            if (maxValue.HasValue && result > maxValue.Value)
            {
                return defaultValue;
            }
            return result;
        }
        return defaultValue;
    }
}
