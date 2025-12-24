//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace Artizan.IoTHub.Things;

///// <summary>
///// 值包装器泛型基类（对应 Java ValueWrapper<T>）
///// </summary>
///// <typeparam name="T">包装的值类型</typeparam>
//public class ValueWrapper<T>
//{
//    private const string TAG = "[Tmp]ValueWrapper";
//    private static MethodInfo _isIntegralMethod;

//    /// <summary>
//    /// 数据类型标识（int/bool/string/struct/array 等）
//    /// </summary>
//    public string Type { get; set; }

//    /// <summary>
//    /// 包装的实际值
//    /// </summary>
//    public T Value { get; set; }

//    /// <summary>
//    /// 构造函数：带初始值
//    /// </summary>
//    /// <param name="valuePar">初始值</param>
//    public ValueWrapper(T valuePar)
//    {
//        Value = valuePar;
//    }

//    /// <summary>
//    /// 无参构造函数
//    /// </summary>
//    public ValueWrapper()
//    {
//    }

//    /// <summary>
//    /// 检查 JsonPrimitive 是否为整数类型（反射调用 Gson 内部方法）
//    /// </summary>
//    /// <param name="primitive">JsonPrimitive 实例</param>
//    /// <returns>是否为整数</returns>
//    public static bool IsIntegral(JValue primitive)
//    {
//        try
//        {
//            if (_isIntegralMethod == null)
//            {
//                // 注意：C# 中无直接对应的 Gson.JsonPrimitive，此处适配 Newtonsoft.Json 的 JValue
//                // 若需完全兼容 Java 逻辑，需替换为 Gson for .NET 的类型
//                Type jsonPrimitiveType = Type.GetType("Google.Gson.JsonPrimitive, Google.Gson");
//                if (jsonPrimitiveType != null)
//                {
//                    _isIntegralMethod = jsonPrimitiveType.GetMethod("isIntegral",
//                        BindingFlags.NonPublic | BindingFlags.Static,
//                        null,
//                        new[] { jsonPrimitiveType },
//                        null);
//                    _isIntegralMethod?.SetAccessible(true);
//                }
//            }

//            if (_isIntegralMethod != null)
//            {
//                return (bool)_isIntegralMethod.Invoke(null, new[] { primitive });
//            }
//            return false;
//        }
//        catch (Exception ex)
//        {
//            // 替代 Java 的 printStackTrace()
//            Console.WriteLine($"[ERROR][{TAG}] IsIntegral error: {ex}");
//            return false;
//        }
//    }

//    /// <summary>
//    /// 检查 JsonPrimitive 字符串是否为整数（无小数点/科学计数法）
//    /// </summary>
//    /// <param name="primitive">JsonPrimitive 实例</param>
//    /// <returns>是否为整数格式</returns>
//    public static bool IsInteger(JValue primitive)
//    {
//        string numberStr = primitive.ToString();
//        return !numberStr.Contains(".") && !numberStr.Contains("e") && !numberStr.Contains("E");
//    }

//    #region 嵌套序列化/反序列化器（适配 Newtonsoft.Json）
//    /// <summary>
//    /// ValueWrapper 反序列化器（对应 Java ValueWrapperJsonDeSerializer）
//    /// </summary>
//    public class ValueWrapperJsonDeSerializer : JsonConverter<ValueWrapper<object>>
//    {
//        public override ValueWrapper<object> ReadJson(JsonReader reader, Type objectType, ValueWrapper<object> existingValue, bool hasExistingValue, JsonSerializer serializer)
//        {
//            if (reader.TokenType == JsonToken.Null)
//            {
//                return null;
//            }

//            JToken json = JToken.Load(reader);
//            ValueWrapper<object> valueWrapper = null;

//            if (json.Type == JTokenType.Object)
//            {
//                var structWrapper = new StructValueWrapper();
//                JObject jsonObject = (JObject)json;
//                foreach (var prop in jsonObject.Properties())
//                {
//                    var childWrapper = serializer.Deserialize<ValueWrapper<object>>(prop.Value.CreateReader());
//                    structWrapper.AddValue(prop.Name, childWrapper);
//                }
//                valueWrapper = structWrapper;
//            }
//            else if (json.Type == JTokenType.Array)
//            {
//                var arrayWrapper = new ArrayValueWrapper();
//                JArray jsonArray = (JArray)json;
//                foreach (JToken item in jsonArray)
//                {
//                    var childWrapper = serializer.Deserialize<ValueWrapper<object>>(item.CreateReader());
//                    arrayWrapper.Add(childWrapper);
//                }
//                valueWrapper = arrayWrapper;
//            }
//            else if (json.Type == JTokenType.Primitive)
//            {
//                JValue jsonPrimitive = (JValue)json;
//                if (jsonPrimitive == null) return null;

//                if (jsonPrimitive.Type == JTokenType.Boolean)
//                {
//                    // Java 中 bool 存储为 int（1/0），此处适配
//                    valueWrapper = new BooleanValueWrapper(jsonPrimitive.Value<bool>() ? 1 : 0);
//                }
//                else if (jsonPrimitive.Type == JTokenType.String)
//                {
//                    valueWrapper = new StringValueWrapper(jsonPrimitive.Value<string>());
//                }
//                else if (jsonPrimitive.Type == JTokenType.Float || jsonPrimitive.Type == JTokenType.Integer)
//                {
//                    if (IsInteger(jsonPrimitive))
//                    {
//                        valueWrapper = new IntValueWrapper(jsonPrimitive.Value<int>());
//                    }
//                    else
//                    {
//                        valueWrapper = new DoubleValueWrapper(jsonPrimitive.Value<double>());
//                    }
//                }
//            }

//            return valueWrapper;
//        }

//        public override void WriteJson(JsonWriter writer, ValueWrapper<object> value, JsonSerializer serializer)
//        {
//            // 序列化逻辑由 ValueWrapperJsonSerializer 实现
//            throw new NotImplementedException();
//        }
//    }

//    /// <summary>
//    /// ValueWrapper 序列化器（对应 Java ValueWrapperJsonSerializer）
//    /// </summary>
//    public class ValueWrapperJsonSerializer : JsonConverter<ValueWrapper<object>>
//    {
//        public override void WriteJson(JsonWriter writer, ValueWrapper<object> src, JsonSerializer serializer)
//        {
//            if (src == null)
//            {
//                writer.WriteNull();
//                return;
//            }

//            switch (src.Type?.ToLower())
//            {
//                case "int":
//                case "enum":
//                    writer.WriteValue((int)src.Value);
//                    break;
//                case "string":
//                case "date":
//                    writer.WriteValue((string)src.Value);
//                    break;
//                case "bool":
//                    writer.WriteValue((int)src.Value); // 保持 Java 兼容：bool 存为 int
//                    break;
//                case "double":
//                case "float":
//                    writer.WriteValue((double)src.Value);
//                    break;
//                case "array":
//                    var arrayList = (List<ValueWrapper<object>>)src.Value;
//                    writer.WriteStartArray();
//                    if (arrayList != null && arrayList.Any())
//                    {
//                        foreach (var item in arrayList)
//                        {
//                            serializer.Serialize(writer, item);
//                        }
//                    }
//                    else
//                    {
//                        ALog.e(TAG, "TYPE_VALUE_ARRAY empty return []");
//                    }
//                    writer.WriteEndArray();
//                    break;
//                case "struct":
//                    var structDict = (Dictionary<string, ValueWrapper<object>>)src.Value;
//                    writer.WriteStartObject();
//                    if (structDict != null && structDict.Any())
//                    {
//                        foreach (var kvp in structDict)
//                        {
//                            writer.WritePropertyName(kvp.Key);
//                            serializer.Serialize(writer, kvp.Value);
//                        }
//                    }
//                    writer.WriteEndObject();
//                    break;
//                default:
//                    serializer.Serialize(writer, src.Value);
//                    break;
//            }
//        }

//        public override ValueWrapper<object> ReadJson(JsonReader reader, Type objectType, ValueWrapper<object> existingValue, bool hasExistingValue, JsonSerializer serializer)
//        {
//            // 反序列化逻辑由 ValueWrapperJsonDeSerializer 实现
//            throw new NotImplementedException();
//        }
//    }
//    #endregion

//    #region 具体类型的 ValueWrapper 子类
//    /// <summary>
//    /// 结构体类型包装器（对应 Java StructValueWrapper）
//    /// </summary>
//    public class StructValueWrapper : ValueWrapper<Dictionary<string, ValueWrapper<object>>>
//    {
//        public StructValueWrapper() : this(new Dictionary<string, ValueWrapper<object>>())
//        {
//        }

//        public StructValueWrapper(Dictionary<string, ValueWrapper<object>> valueParam) : base(valueParam)
//        {
//            Type = "struct";
//        }

//        public ValueWrapper<object> AddValue(string key, ValueWrapper<object> valueWrapper)
//        {
//            if (Value == null)
//            {
//                Value = new Dictionary<string, ValueWrapper<object>>();
//            }

//            Value[key] = valueWrapper;
//            return valueWrapper;
//        }
//    }

//    /// <summary>
//    /// 数组类型包装器（对应 Java ArrayValueWrapper）
//    /// </summary>
//    public class ArrayValueWrapper : ValueWrapper<List<ValueWrapper<object>>>
//    {
//        public ArrayValueWrapper() : base()
//        {
//            Type = "array";
//        }

//        public ArrayValueWrapper(List<ValueWrapper<object>> valueParam) : base(valueParam)
//        {
//            Type = "array";
//        }

//        public void Add(ValueWrapper<object> obj)
//        {
//            if (Value == null)
//            {
//                Value = new List<ValueWrapper<object>>();
//            }
//            Value.Add(obj);
//        }
//    }

//    /// <summary>
//    /// 双精度浮点型包装器（对应 Java DoubleValueWrapper）
//    /// </summary>
//    public class DoubleValueWrapper : ValueWrapper<double>
//    {
//        public DoubleValueWrapper() : base()
//        {
//            Type = "double";
//        }

//        public DoubleValueWrapper(double valueParam) : base(valueParam)
//        {
//            Type = "double";
//        }
//    }

//    /// <summary>
//    /// 布尔类型包装器（继承 IntValueWrapper，对应 Java BooleanValueWrapper）
//    /// </summary>
//    public class BooleanValueWrapper : IntValueWrapper
//    {
//        public BooleanValueWrapper() : base()
//        {
//            Type = "bool";
//        }

//        public BooleanValueWrapper(int valueParam) : base(valueParam)
//        {
//            Type = "bool";
//        }
//    }

//    /// <summary>
//    /// 字符串类型包装器（对应 Java StringValueWrapper）
//    /// </summary>
//    public class StringValueWrapper : ValueWrapper<string>
//    {
//        public StringValueWrapper() : base()
//        {
//            Type = "string";
//        }

//        public StringValueWrapper(string valuePar) : base(valuePar)
//        {
//            Type = "string";
//        }
//    }

//    /// <summary>
//    /// 枚举类型包装器（继承 IntValueWrapper，对应 Java EnumValueWrapper）
//    /// </summary>
//    public class EnumValueWrapper : IntValueWrapper
//    {
//        public EnumValueWrapper() : base()
//        {
//            Type = "enum";
//        }

//        public EnumValueWrapper(int valuePar) : base(valuePar)
//        {
//            Type = "enum";
//        }
//    }

//    /// <summary>
//    /// 日期类型包装器（继承 StringValueWrapper，对应 Java DateValueWrapper）
//    /// </summary>
//    public class DateValueWrapper : StringValueWrapper
//    {
//        public DateValueWrapper() : base()
//        {
//            Type = "date";
//        }

//        public DateValueWrapper(string valuePar) : base(valuePar)
//        {
//            Type = "date";
//        }
//    }

//    /// <summary>
//    /// 整数类型包装器（对应 Java IntValueWrapper）
//    /// </summary>
//    public class IntValueWrapper : ValueWrapper<int>
//    {
//        public IntValueWrapper() : base()
//        {
//            Type = "int";
//        }

//        public IntValueWrapper(int valueParam) : base(valueParam)
//        {
//            Type = "int";
//        }
//    }
//    #endregion
//}
