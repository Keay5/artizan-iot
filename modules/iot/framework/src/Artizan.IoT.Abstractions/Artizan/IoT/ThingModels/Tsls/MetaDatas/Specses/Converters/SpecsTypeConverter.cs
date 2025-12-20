using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters
{
    public static class SpecsTypeConverter
    {
        #region （1）策略字典定义（缓存映射关系）

        // 取值策略
        // 策略字典：规格类型 → 取值策略（从规格中获取指定key的值）
        private static readonly Dictionary<Type, Func<ISpecs, string, object>> _getValueStrategies = new()
        {
            { typeof(NumericSpecs), GetNumericSpecsValue },
            { typeof(KeyValueSpecs), GetKeyValueSpecsValue },
            { typeof(StringSpecs), GetStringSpecsValue },
            { typeof(ArraySpecs), GetArraySpecsValue },
            { typeof(StructSpecs), (_, _) => throw new NotSupportedException("StructSpecs不支持GetValue。") },
            { typeof(EmptySpecs), (_, _) => throw new NotSupportedException("EmptySpecs不支持GetValue。") }
        };

        // 赋值策略
        // 策略字典：规格类型 → 赋值策略（向规格中设置指定key的值）
        private static readonly Dictionary<Type, Action<ISpecs, string, string>> _setValueStrategies = new()
        {
            { typeof(NumericSpecs), SetNumericSpecsValue },
            { typeof(KeyValueSpecs), SetKeyValueSpecsValue },
            { typeof(StringSpecs), SetStringSpecsValue },
            { typeof(ArraySpecs), SetArraySpecsValue },
            { typeof(StructSpecs), (_, _, _) => throw new NotSupportedException("StructSpecs不支持SetValue。") },
            { typeof(EmptySpecs), (_, _, _) => throw new NotSupportedException("EmptySpecs不支持SetValue。") }
        };

        #region （5）缓存 TypeConverter（性能优化）

        private static readonly ConcurrentDictionary<Type, TypeConverter> _converterCache = new(); 

        #endregion

        #endregion

        #region （2）自动匹配策略的核心方法

        #region 核心方法：GetValue<T>: 取值策略自动匹配

        /// <summary>
        /// 从规格中获取强类型值(（自动匹配策略）)
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="specs">规格实例</param>
        /// <param name="key">属性键</param>
        /// <param name="allowNull">是否允许空值（默认不允许）</param>
        /// <returns>强类型值</returns>
        public static T GetValue<T>(this ISpecs specs, string key, bool allowNull = false)
        {
            if (specs == null)
            {
                throw new ArgumentNullException(nameof(specs));
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            // 1. 获取规格类型（如NumericSpecs）
            var specType = specs.GetType();

            // 2. 自动匹配策略：从字典中查找该类型对应的取值策略
            if (!_getValueStrategies.TryGetValue(specType, out var strategy))
            {
                //throw new NotSupportedException($"GetValue not supported for {specType.Name}");
                throw new NotSupportedException($"不支持{specType.Name}的GetValue操作");
            }

            // 3. 执行策略并返回结果
            var value = strategy(specs, key);

            // 4.// 转换为目标类型
            return ConvertValue<T>(value, allowNull);
        }
        #endregion

        #region 核心方法：SetValue<T>:赋值策略自动匹配
        /// <summary>
        /// 设置规格的强类型值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="specs">规格实例</param>
        /// <param name="key">属性键</param>
        /// <param name="value">强类型值</param>
        /// <param name="allowNull">是否允许空值（默认不允许）</param>
        public static void SetValue<T>(this ISpecs specs, string key, T value, bool allowNull = false)
        {
            if (specs == null)
                throw new ArgumentNullException(nameof(specs));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (value == null && !allowNull)
                throw new ArgumentNullException(nameof(value), "Value cannot be null");

            // 统一转换为字符串
            string stringValue;
            if (value == null)
            {
                stringValue = null;
            }
            else if (value is IFormattable formattable)
            {
                // 对支持自定义格式化的类型，直接使用InvariantCulture。（使用InvariantCulture避免格式问题）
                stringValue = formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            else
            {
                // 其他类型使用默认ToString
                stringValue = value.ToString();
            }

            var specType = specs.GetType();
            if (!_setValueStrategies.TryGetValue(specType, out var strategy))
                throw new NotSupportedException($"SetValue not supported for {specType.Name}");

            // 策略模式
            strategy(specs, key, stringValue);
        }
        #endregion 

	    #endregion

        #region （4）注册自定义策略（动态扩展新策略）

        /// <summary>
        /// 注册自定义GetValue策略:注册自定义规格类型的取值策略
        /// </summary>
        /// <param name="specType">规格类型</param>
        /// <param name="strategy"></param>
        /// 
        /// 示例：为自定义的DecimalSpecs注册策略
        ///  SpecsTypeConverter.RegisterGetValueStrategy(
        ///     typeof(DecimalSpecs), 
        ///     (specs, key) => 
        ///     {
        ///         var decimalSpecs = (DecimalSpecs)specs;
        ///         return key switch 
        ///         { 
        ///             "precision" => decimalSpecs.Precision, 
        ///             _ => throw new KeyNotFoundException($"DecimalSpecs中不存在键{key}")
        ///         };
        ///     }
        ///  );
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterGetValueStrategy(Type specType, Func<ISpecs, string, object> strategy)
        {
            ValidateSpecType(specType);
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            _getValueStrategies[specType] = strategy;
        }

        /// <summary>
        /// 注册自定义SetValue策略
        /// </summary>
        public static void RegisterSetValueStrategy(Type specType, Action<ISpecs, string, string> strategy)
        {
            ValidateSpecType(specType);
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            _setValueStrategies[specType] = strategy;
        }

        private static void ValidateSpecType(Type specType)
        {
            if (specType == null)
            {
                throw new ArgumentNullException(nameof(specType));
            }

            if (!typeof(ISpecs).IsAssignableFrom(specType))
            {
                throw new ArgumentException("Type must implement ISpecs", nameof(specType));
            }
        }
        #endregion

        #region 私有方法：类型转换（性能+空值优化）
        /// <summary>
        /// 安全转换值类型
        /// </summary>
        private static T? ConvertValue<T>(object value, bool allowNull)
        {
            // 空值处理
            if (value == null || value == DBNull.Value)
            {
                if (allowNull)
                {
                    return default;
                }

                throw new InvalidCastException("Value cannot be null");
            }

            // 直接类型匹配，无需转换
            if (value is T typedValue)
            {
                return typedValue;
            }

            // 从缓存获取TypeConverter
            var targetType = typeof(T);
            if (!_converterCache.TryGetValue(targetType, out var converter))
            {
                converter = TypeDescriptor.GetConverter(targetType);
                _converterCache.TryAdd(targetType, converter);
            }

            try
            {
                // 使用InvariantCulture确保跨系统转换一致性
                return (T)converter.ConvertFromInvariantString(value.ToString());
            }
            catch (NotSupportedException)
            {
                // 兜底：基础类型强制转换
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Cannot convert '{value}' (type: {value.GetType().Name}) to {targetType.Name}",
                    ex);
            }
        }
        #endregion

        #region （3）策略实现：各类型策略实现（每个策略方法处理特定规格类型的解析逻辑）

        // NumericSpecs取值策略
        private static object GetNumericSpecsValue(ISpecs specs, string key)
        {
            var numeric = (NumericSpecs)specs;
            return key switch
            {
                "min" => numeric.Min,
                "max" => numeric.Max,
                "step" => numeric.Step,
                "unit" => numeric.Unit,
                "unitName" => numeric.UnitName,
                _ => throw new KeyNotFoundException($"Key '{key}' not found in NumericSpecs")
            };
        }

        // KeyValueSpecs取值策略
        private static object GetKeyValueSpecsValue(ISpecs specs, string key)
        {
            var kv = (KeyValueSpecs)specs;
            return kv.GetValue(key) ?? throw new KeyNotFoundException($"Key '{key}' not found in KeyValueSpecs");
        }

        // StringSpecs取值策略
        private static object GetStringSpecsValue(ISpecs specs, string key)
        {
            var str = (StringSpecs)specs;
            return key == "length"
                ? str.Length
                : throw new KeyNotFoundException($"Key '{key}' not found in StringSpecs");
        }

        // ArraySpecs取值策略
        private static object GetArraySpecsValue(ISpecs specs, string key)
        {
            var arr = (ArraySpecs)specs;
            return key == "size"
                ? arr.Size
                : throw new KeyNotFoundException($"Key '{key}' not found in ArraySpecs");
        }

        // NumericSpecs赋值策略
        private static void SetNumericSpecsValue(ISpecs specs, string key, string? value)
        {
            var numeric = (NumericSpecs)specs;
            switch (key)
            {
                case "min": numeric.Min = value; break;
                case "max": numeric.Max = value; break;
                case "step": numeric.Step = value; break;
                case "unit": numeric.Unit = value; break;
                case "unitName": numeric.UnitName = value; break;
                default: throw new KeyNotFoundException($"Key '{key}' not found in NumericSpecs");
            }
        }

        // KeyValueSpecs赋值策略
        private static void SetKeyValueSpecsValue(ISpecs specs, string key, string? value)
        {
            var kvSpecs = (KeyValueSpecs)specs;
            kvSpecs.SetValue(key, value);
        }

        // StringSpecs赋值策略
        private static void SetStringSpecsValue(ISpecs specs, string key, string value)
        {
            var strSpecs = (StringSpecs)specs;
            if (key == "length")
                strSpecs.SetLength(value);
            else
                throw new KeyNotFoundException($"Key '{key}' not found in StringSpecs");
        }

        // ArraySpecs赋值策略
        private static void SetArraySpecsValue(ISpecs specs, string key, string value)
        {
            var arrSpecs = (ArraySpecs)specs;
            if (key == "size")
                arrSpecs.SetSzie(value);
            else
                throw new KeyNotFoundException($"Key '{key}' not found in ArraySpecs");
        }
        #endregion
    }
}