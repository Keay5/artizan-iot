using JetBrains.Annotations;
using System;
using System.Runtime.Serialization;
using Volo.Abp;

namespace Artizan.IoT.Errors;

/// <summary>
/// IoT错误信息实体（值对象模式）
/// 设计模式：值对象（Value Object）
/// 设计思路：
/// 1. 无唯一标识，仅通过属性值（Code+Description）判断相等
/// 2. 作为错误传递的最小单元，封装错误的核心信息，不包含业务逻辑
/// 设计考量：
/// - 错误码强制必填（通过Check.NotNullOrEmpty），保证每个错误有唯一标识
/// - Description可选，用于存储本地化参数（如设备ID"Sensor001"），非最终展示文本
/// - 重写Equals/GetHashCode，支持集合去重（如IoTResult.Combine方法）
/// - 不可变性：属性仅可读，构造时赋值，避免外部篡改
/// - 序列化支持：标记DataContract，适配分布式场景
/// - 实现接口 IEquatable{IoTError}: 默认情况下，.NET 中引用类型（class）的相等性判断是「引用相等」（判断两个对象是否指向内存中同一个地址），
///     而 IoTError 作为「错误码 + 描述」的业务对象，我们需要的是「值相等」（只要 Code 和 Description 相同，就认为两个 IoTError 相等）
///     ——IEquatable<IoTError> 正是为解决这个核心问题而生。
/// </summary>
[Serializable]
[DataContract]
public class IoTError : IEquatable<IoTError>
{
    /// <summary>
    /// 错误码（关联<see cref="IoTErrorCodes"/>）
    /// </summary>
    [DataMember]
    public string Code { get; }

    /// <summary>
    /// 错误描述（可存储本地化参数，如设备ID、用户ID等）
    /// </summary>
    [DataMember]
    public string? Description { get; }

    /// <summary>
    /// 构造函数（强制错误码必填）
    /// </summary>
    /// <param name="code">错误码（非空）</param>
    /// <param name="description">错误描述（参数）</param>
    /// <exception cref="ArgumentNullException">错误码为空时抛出</exception>
    public IoTError([NotNull] string code, string? description = null)
    {
        Code = Check.NotNullOrEmpty(code, nameof(code));
        Description = description;
    }

    #region IEquatable<IoTError> 实现 等值判断（值对象核心特性）

    /// <summary>
    /// 强类型相等性判断（核心：仅比较 Code，Description 不影响错误唯一性）
    /// </summary>
    public bool Equals(IoTError? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Code == other.Code && Description == other.Description;
    }

    /// <summary>
    /// 重写 object.Equals，委托给强类型 Equals
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != GetType())
        {
            return false;
        }
        return Equals((IoTError)obj);
    }

    /// <summary>
    /// 重写 GetHashCode（必须与 Equals 逻辑一致，否则集合类会出错）
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Description);
    }

    /// <summary>
    /// 重载 == 运算符，符合直觉
    /// </summary>
    public static bool operator ==(IoTError? left, IoTError? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// 重载 != 运算符，符合直觉
    /// </summary>
    public static bool operator !=(IoTError? left, IoTError? right)
    {
        return !Equals(left, right);
    }

    #endregion

    /// <summary>
    /// 重写ToString，便于日志/调试输出（仅格式化，不记录）
    /// </summary>
    /// <returns>错误码+描述的可读格式</returns>
    public override string ToString()
    {
        return $"Code: {Code}, Description: {Description ?? "N/A"}";
    }
}