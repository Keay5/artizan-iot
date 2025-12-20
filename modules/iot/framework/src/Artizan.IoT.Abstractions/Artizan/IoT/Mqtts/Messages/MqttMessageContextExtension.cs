using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Messages;

/// <summary>
/// 上下文扩展字段（避免频繁修改上下文类，用键值对承载自定义数据）
/// </summary>
public class MqttMessageContextExtension
{
    private readonly ConcurrentDictionary<string, object> _data = new();

    /// <summary>
    /// 设置扩展字段（线程安全）
    /// </summary>
    public void Set(string key, object value) => _data[key] = value;

    /// <summary>
    /// 获取扩展字段（泛型，避免类型转换）
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_data.TryGetValue(key, out var value) && value is T tValue)
        {
            return tValue;
        }
        return default;
    }

    /// <summary>
    /// 移除扩展字段
    /// </summary>
    public bool Remove(string key) => _data.TryRemove(key, out _);

    /// <summary>
    /// 转换为字典（用于日志输出）
    /// </summary>
    public IReadOnlyDictionary<string, object> ToDictionary() => _data;
}
