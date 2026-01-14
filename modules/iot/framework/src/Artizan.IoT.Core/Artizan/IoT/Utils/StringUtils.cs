using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Utils;

public static class StringUtils
{
    /// <summary>
    /// 计算字符长度（中文算2个字符，其他算1个）
    /// </summary>
    public static int CalculateCharacterLength(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        int length = 0;
        foreach (char c in input)
        {
            // 严格判断是否为中文（Unicode基本多文种平面中的汉字）
            // 覆盖基本汉字（\u4e00-\u9fa5）+扩展汉字（\u9fa6-\u9fff）
            if (char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter &&
                c >= '\u4e00' && c <= '\u9fa5')
            {
                length += 2;
            }
            // 排除不可见字符（如空格、制表符、控制字符）
            else if (!char.IsControl(c) && !char.IsWhiteSpace(c))
            {
                length += 1;
            }
        }
        return length;
    }
}
