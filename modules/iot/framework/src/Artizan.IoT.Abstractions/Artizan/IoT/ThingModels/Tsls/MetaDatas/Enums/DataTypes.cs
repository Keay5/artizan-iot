using System.Runtime.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

public enum DataTypes
{
    /// <summary>
    /// 32位整型。需定义取值范围、步长和单位符号。
    /// </summary>
    [EnumMember(Value = "int")]
    Int32,
    /// <summary>
    /// 单精度浮点型。需定义取值范围、步长和单位符号。
    /// </summary>
    [EnumMember(Value = "float")]
    Float,
    /// <summary>
    /// 双精度浮点型。需定义取值范围、步长和单位符号。
    /// </summary>
    [EnumMember(Value = "double")]
    Double,
    /// <summary>
    /// 布尔型。采用0或1来定义布尔值，例如：0表示关、1表示开。
    /// </summary>
    [EnumMember(Value = "bool")]
    Boolean,
    /// <summary>
    /// 枚举型。定义枚举项的参数值和参数描述，例如：1表示加热模式、2表示制冷模式。
    /// </summary>
    [EnumMember(Value = "enum")]
    Enum,
    /// <summary>
    /// 字符串。需定义字符串的数据长度，最长支持10240字节。
    /// </summary>
    [EnumMember(Value = "text")]
    Text,
    /// <summary>
    /// 时间戳。格式为String类型的UTC时间戳，单位：毫秒。
    /// </summary>
    /// 
    [EnumMember(Value = "date")]
    Date,
    /// <summary>
    /// 数组类型，其元素类型见有限制
    /// </summary>
    [EnumMember(Value = "array")]
    Array,
    /// <summary>
    /// JSON对象。定义一个JSON结构体，新增JSON参数项，例如：定义灯的颜色是由Red、Green、Blue三个参数组成的结构体。不支持结构体嵌套。
    /// </summary>
    [EnumMember(Value = "struct")]
    Struct

}
