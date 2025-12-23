using System;
using System.Collections.Generic;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// Alink上下文扩展容器（键值对形式，支持任意类型）
/// 【设计理念】：开放封闭原则，避免频繁修改上下文类结构
/// </summary>
public class AlinkContextExtension
{
    private readonly Dictionary<string, object> _extensions = new();

    /// <summary>
    /// 添加扩展数据
    /// </summary>
    public void Set<T>(string key, T value) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "扩展字段Key不能为空");
        }
        _extensions[key] = value;
    }

    /// <summary>
    /// 获取扩展数据（不存在返回默认值）
    /// </summary>
    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (!_extensions.TryGetValue(key, out var value))
        {
            return defaultValue;
        }
        return value is T t ? t : defaultValue;
    }

    /// <summary>
    /// 尝试获取扩展数据
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        if (!_extensions.TryGetValue(key, out var obj))
        {
            return false;
        }
        if (obj is T t)
        {
            value = t;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 移除扩展数据
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "扩展字段Key不能为空");
        }
        return _extensions.Remove(key);
    }

    /// <summary>
    /// 清空所有扩展数据
    /// </summary>
    public void Clear()
    {
        _extensions.Clear();
    }
}

