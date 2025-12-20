using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

/// <summary>
///  键值规格，适用于：布尔、枚举。支持扩展数据。
/// </summary>
public class KeyValueSpecs : ISpecs
{
    // 扩展数据必须使用IDictionary<string, JToken>类型
    [JsonExtensionData]
    private IDictionary<string, JToken> _extensionData = new Dictionary<string, JToken>();

    // 提供强类型访问接口
    [JsonIgnore] // 关键：忽略序列化
    public Dictionary<string, string> Values
    {
        get
        {
            var dict = new Dictionary<string, string>();
            foreach (var kvp in _extensionData)
            {
                dict[kvp.Key] = kvp.Value.ToString();
            }
            return dict;
        }
        set
        {
            _extensionData.Clear();
            foreach (var kvp in value)
            {
                _extensionData[kvp.Key] = JToken.FromObject(kvp.Value);
            }
        }
    }

    // 
    /// <summary>
    /// 供扩展方法调用的 GetValue
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return _extensionData.TryGetValue(key, out var token) ? token.ToString() : null;
    }

    /// <summary>
    /// // 供扩展方法调用的 SetValue
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value">赋值null时, 将Remove 操作</param>
    public void SetValue(string key, string? value)
    {
        if (value == null)
            _extensionData.Remove(key);
        else
            _extensionData[key] = JToken.FromObject(value);
    }
}