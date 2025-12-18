using Newtonsoft.Json;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

/// <summary>
/// 字符串规格
/// </summary>
public class StringSpecs : ISpecs
{
    [JsonProperty("length", Order = 1)]
    public string Length { get; protected set; }

    protected StringSpecs()
    {
    }

    public StringSpecs(string length)
    {
        SetLength(length);
    }

    public StringSpecs SetLength(string length)
    {
        if (length.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(length), "字符串长度不能为空");
        }

        int lengthVal = 0;
        // 校验 length 为非负整数
        if (!string.IsNullOrEmpty(length) && !int.TryParse(length, out lengthVal) || lengthVal < 0)
        {
            throw new ArgumentException($"字符串长度{length}必须为非负整数");
        }

        Length = length;

        return this;
    }
}
