## 问：如何在策略模式中实现自动匹配参数解析策略？

在策略模式中实现参数解析策略的自动匹配，核心是通过**类型映射**和**字典缓存**建立规格类型与解析策略的关联，结合反射或类型判断实现自动绑定。以下是基于代码库现有`SpecsTypeConverter`的实现方案：

### 1. 核心思路

- **策略注册**：提前将规格类型（如`NumericSpecs`、`KeyValueSpecs`）与对应的解析策略（取值 / 赋值方法）注册到字典中。
- **自动匹配**：根据输入的规格实例类型，从字典中自动查找并执行对应的策略，无需手动判断类型。
- **扩展性**：通过`RegisterGetValueStrategy`和`RegisterSetValueStrategy`方法支持动态添加新策略。

### 2. 实现代码解析



#### （1）策略字典定义（缓存映射关系）

在`SpecsTypeConverter`中定义两个字典，分别存储**取值策略**和**赋值策略**，键为规格类型（`Type`），值为对应的处理函数：

```csharp
// 策略字典：规格类型 → 取值策略（从规格中获取指定key的值）
private static readonly Dictionary<Type, Func<ISpecs, string, object>> _getValueStrategies = new()
{
    { typeof(NumericSpecs), GetNumericSpecsValue },
    { typeof(KeyValueSpecs), GetKeyValueSpecsValue },
    { typeof(StringSpecs), GetStringSpecsValue },
    { typeof(ArraySpecs), GetArraySpecsValue },
    { typeof(StructSpecs), (_, _) => throw new NotSupportedException("StructSpecs不支持GetValue") },
    { typeof(EmptySpecs), (_, _) => throw new NotSupportedException("EmptySpecs不支持GetValue") }
};

// 策略字典：规格类型 → 赋值策略（向规格中设置指定key的值）
private static readonly Dictionary<Type, Action<ISpecs, string, string>> _setValueStrategies = new()
{
    { typeof(NumericSpecs), SetNumericSpecsValue },
    { typeof(KeyValueSpecs), SetKeyValueSpecsValue },
    { typeof(StringSpecs), SetStringSpecsValue },
    { typeof(ArraySpecs), SetArraySpecsValue },
    { typeof(StructSpecs), (_, _, _) => throw new NotSupportedException("StructSpecs不支持SetValue") },
    { typeof(EmptySpecs), (_, _, _) => throw new NotSupportedException("EmptySpecs不支持SetValue") }
};
```



#### （2）自动匹配策略的核心方法

通过规格实例的类型（`specs.GetType()`）从字典中查找对应策略，并执行：



###### 取值策略自动匹配

```csharp
/// <summary>
/// 从规格中获取值（自动匹配策略）
/// </summary>
public static T GetValue<T>(this ISpecs specs, string key, bool allowNull = false)
{
    if (specs == null) throw new ArgumentNullException(nameof(specs));
    if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

    // 1. 获取规格类型（如NumericSpecs）
    var specType = specs.GetType();

    // 2. 自动匹配策略：从字典中查找该类型对应的取值策略
    if (!_getValueStrategies.TryGetValue(specType, out var strategy))
    {
        throw new NotSupportedException($"不支持{specType.Name}的GetValue操作");
    }

    // 3. 执行策略并返回结果
    var value = strategy(specs, key);
    return ConvertValue<T>(value, allowNull); // 转换为目标类型
}
```



###### 赋值策略自动匹配

```csharp
/// <summary>
/// 向规格中设置值（自动匹配策略）
/// </summary>
public static void SetValue<T>(this ISpecs specs, string key, T value, bool allowNull = false)
{
    if (specs == null) throw new ArgumentNullException(nameof(specs));
    if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
    if (value == null && !allowNull) throw new ArgumentNullException(nameof(value));

    // 1. 转换值为字符串（统一格式）
    string stringValue = ConvertToString(value);

    // 2. 获取规格类型（如KeyValueSpecs）
    var specType = specs.GetType();

    // 3. 自动匹配策略：从字典中查找该类型对应的赋值策略
    if (!_setValueStrategies.TryGetValue(specType, out var strategy))
    {
        throw new NotSupportedException($"不支持{specType.Name}的SetValue操作");
    }

    // 4. 执行策略
    strategy(specs, key, stringValue);
}
```



#### （3）策略实现（以数值型和键值型为例）

每个策略方法处理特定规格类型的解析逻辑：

```csharp
// NumericSpecs取值策略（处理min/max/step等参数）
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
        _ => throw new KeyNotFoundException($"NumericSpecs中不存在键{key}")
    };
}

// KeyValueSpecs赋值策略（处理布尔/枚举的键值对）
private static void SetKeyValueSpecsValue(ISpecs specs, string key, string? value)
{
    var kv = (KeyValueSpecs)specs;
    kv.SetValue(key, value); // 调用KeyValueSpecs的内部方法
}
```

#### （4）动态扩展新策略

通过`RegisterGetValueStrategy`和`RegisterSetValueStrategy`方法，可在运行时添加自定义规格类型的解析策略，无需修改原有代码：

```csharp
// 注册自定义规格类型的取值策略
public static void RegisterGetValueStrategy(Type specType, Func<ISpecs, string, object> strategy)
{
    ValidateSpecType(specType); // 验证规格类型是否实现ISpecs
    _getValueStrategies[specType] = strategy ?? throw new ArgumentNullException(nameof(strategy));
}

// 示例：为自定义的DecimalSpecs注册策略
SpecsTypeConverter.RegisterGetValueStrategy(
    typeof(DecimalSpecs), 
    (specs, key) => 
    {
        var decimalSpecs = (DecimalSpecs)specs;
        return key switch 
        { 
            "precision" => decimalSpecs.Precision, 
            _ => throw new KeyNotFoundException($"DecimalSpecs中不存在键{key}") 
        };
    }
);
```



### 3. 使用示例

当需要解析不同类型的规格时，无需手动判断类型，直接调用`GetValue`或`SetValue`即可自动匹配策略：

```csharp
// 1. 数值型规格解析（自动匹配NumericSpecs策略）
var numericSpecs = new NumericSpecs { Min = "1", Max = "5" };
var minValue = numericSpecs.GetValue<int>("min"); // 自动调用GetNumericSpecsValue

// 2. 键值型规格解析（自动匹配KeyValueSpecs策略）
var kvSpecs = new KeyValueSpecs();
kvSpecs.SetValue("0", "关闭"); // 自动调用SetKeyValueSpecsValue
var value0 = kvSpecs.GetValue<string>("0"); // 自动调用GetKeyValueSpecsValue
```



### 4. 优势总结

- **自动匹配**：通过类型映射实现策略的自动绑定，减少手动`if-else`判断。
- **可扩展性**：支持动态注册新策略，符合开闭原则。
- **类型安全**：通过泛型方法`GetValue`和`SetValue`确保参数类型正确。
- **一致性**：所有规格类型的解析逻辑集中管理，便于维护。

