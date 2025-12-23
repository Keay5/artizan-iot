using System;
using System.Collections.Generic;

namespace Artizan.IoT.Commons.Extensions;

/// <summary>
/// Dictionary扩展方法（补充AddRange，简化多字段添加）
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// 批量添加键值对（覆盖已有键，避免重复键异常）
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="target">目标字典</param>
    /// <param name="source">待添加的源字典</param>
    /// <exception cref="ArgumentNullException">目标/源字典为空时抛出</exception>
    public static void AddOrReplayRange<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> source)
        where TKey : notnull
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), "目标字典不能为空");
        }
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source), "源字典不能为空");
        }

        foreach (KeyValuePair<TKey, TValue> kv in source)
        {
            if (target.ContainsKey(kv.Key))
            {
                target[kv.Key] = kv.Value; // 覆盖已有值（日志场景优先最新值）
            }
            else
            {
                target.Add(kv.Key, kv.Value);
            }
        }
    }
}