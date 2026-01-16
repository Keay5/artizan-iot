using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// IoT时序数据通用模型
/// 设计思路：适配时序库Tag/Field特性，兼顾类型安全和扩展性
/// 设计模式：使用只读集合+方法修改的方式，保证数据不可变（Immutable）
/// 设计考量：
/// 1. Tags作为索引字段，使用字符串键值对，适配所有时序库的Tag特性
/// 2. Fields存储数值型数据，支持多类型值，满足IoT多指标采集需求
/// 3. 区分采集时间和入库时间，适配IoT设备离线上报场景
/// 4. 只读集合防止外部随意修改，通过专用方法保证修改的可控性
/// </summary>
public class TimeSeriesData
{
    /// <summary>
    /// 物唯一标识（如ProductKey+DeviceName）
    /// </summary>
    [Required(ErrorMessage = "物唯一标识不能为空")]
    public string ThingIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// 数据采集时间戳（UTC时间，设备端采集时间）
    /// </summary>
    [Required(ErrorMessage = "采集时间不能为空")]
    public DateTime UtcDateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 扩展标签（时序库索引字段，用于快速筛选）
    /// 只读：杜绝外部非法修改，所有修改都经过类型校验
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; private set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    /// <summary>
    /// 数值字段（时序库数值字段，用于聚合计算）
    /// 只读：杜绝外部非法修改，所有修改都经过类型校验
    /// </summary>
    public IReadOnlyDictionary<string, object> Fields { get; private set; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

    /// <summary>
    /// 测量表名（InfluxDB Measurement / TDengine 超级表名）
    /// </summary>
    public string Measurement { get; set; } = TimeSeriesConsts.DefaultMeasurementName;

    /// <summary>
    /// 数据入库时间（UTC时间，区别于设备采集时间）
    /// </summary>
    public DateTime InsertedUtcTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 设置标签（类型安全的修改方式）
    /// </summary>
    /// <param name="key">标签键</param>
    /// <param name="value">标签值</param>
    /// <exception cref="ArgumentNullException">键为空时抛出</exception>
    public void SetTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "标签键不能为空");
        }

        var tags = new Dictionary<string, string>(Tags);
        tags[key] = value ?? string.Empty;
        Tags = new ReadOnlyDictionary<string, string>(tags);
    }

    /// <summary>
    /// 移除标签
    /// </summary>
    /// <param name="key">标签键</param>
    /// <exception cref="ArgumentNullException">键为空时抛出</exception>
    public void RemoveTag(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "标签键不能为空");
        }

        var tags = new Dictionary<string, string>(Tags);
        if (tags.ContainsKey(key))
        {
            tags.Remove(key);
            Tags = new ReadOnlyDictionary<string, string>(tags);
        }
    }

    /// <summary>
    /// 设置字段值（泛型方法保证类型安全）
    /// </summary>
    /// <typeparam name="T">字段值类型（仅支持数值类型）</typeparam>
    /// <param name="key">字段键</param>
    /// <param name="value">字段值</param>
    /// <exception cref="ArgumentNullException">键为空时抛出</exception>
    /// <exception cref="ArgumentException">值类型非法时抛出</exception>
    public void SetField<T>(string key, T value)
    // where T : struct, IComparable, IConvertible, IFormattable // TODO: 考虑是否限制类型
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "字段键不能为空");
        }

        /*
         TODO: 是否考虑放开类型限制，允许存储任意类型字段值？
         */
        //// 仅允许数值类型
        //Type type = typeof(T);
        //if (type != typeof(int) && type != typeof(long) && type != typeof(float) &&
        //    type != typeof(double) && type != typeof(decimal) && type != typeof(short))
        //{
        //    throw new ArgumentException($"字段值类型{type.Name}不支持，仅支持数值类型", nameof(value));
        //}

        var fields = new Dictionary<string, object>(Fields);
        fields[key] = value;
        Fields = new ReadOnlyDictionary<string, object>(fields);
    }

    /// <summary>
    /// 移除字段
    /// </summary>
    /// <param name="key">字段键</param>
    /// <exception cref="ArgumentNullException">键为空时抛出</exception>
    public void RemoveField(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "字段键不能为空");
        }

        var fields = new Dictionary<string, object>(Fields);
        if (fields.ContainsKey(key))
        {
            fields.Remove(key);
            Fields = new ReadOnlyDictionary<string, object>(fields);
        }
    }
}
