using Artizan.IoT.Errors;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;

namespace Artizan.IoT.Results;

/// <summary>
/// 带返回数据的IoTResult（泛型扩展）
/// 设计模式：泛型扩展模式（Generic Extension）
/// 设计思路：
/// 1. 继承IoTResult，复用基础结果逻辑，扩展“成功返回数据”的场景
/// 2. 适配“查询/操作成功+返回业务数据”的常见业务场景（如查询设备列表、获取设备详情）
/// 设计考量：
/// - 数据属性（Data）仅在成功时有效，失败时为null
/// - 工厂方法与基类保持一致，降低学习成本
/// - 不可变性：Data属性仅可读，构造时赋值，避免外部篡改
/// - 序列化支持：标记DataContract，适配分布式场景
/// </summary>
[Serializable]
[DataContract]
public class IoTResult<T> : IoTResult
{
    /// <summary>
    /// 业务数据（成功时有效，失败时为null）
    /// [DataMember]：自动序列化业务数据
    /// </summary>
    [DataMember]
    public T? Data { get; protected set; }

    /// <summary>
    /// 创建成功结果（带数据）
    /// </summary>
    /// <param name="data">业务数据</param>
    /// <returns>成功的IoTResult{T}</returns>
    public new static IoTResult<T> Success(T data)
    {
        return new IoTResult<T> { Succeeded = true, Data = data };
    }

    /// <summary>
    /// 创建失败结果（带错误）
    /// </summary>
    /// <param name="errors">错误列表</param>
    /// <returns>失败的IoTResult{T}</returns>
    public new static IoTResult<T> Failed(List<IoTError>? errors)
    {
        var result = new IoTResult<T> { Succeeded = false };
        // 调用基类的AddErrors方法（protected），避免直接访问私有字段
        result.AddErrors(errors);

        return result;
    }

    /// <summary>
    /// 创建失败结果（单错误）
    /// </summary>
    /// <param name="error">单个错误</param>
    /// <returns>失败的IoTResult{T}</returns>
    public new static IoTResult<T> Failed(IoTError error)
    {
        return Failed(new[] { error });
    }

    /// <summary>
    /// 创建失败结果（单错误，直接传错误码+描述）
    /// 设计思路：
    /// 1. 简化业务层调用，无需手动创建IoTError对象
    /// 2. 强制错误码非空，保证每个失败结果都有唯一标识
    /// 3. 兼容null描述，适配无需参数的错误场景（如IoT_DeviceIdEmpty）
    /// 设计考量：
    /// - 入参校验：通过Check.NotNullOrEmpty确保错误码有效，避免空错误码
    /// - 代码复用：内部调用已有的Failed(IoTError[])方法，保证错误过滤逻辑一致
    /// </summary>
    /// <param name="code">错误码（必须非空，关联<see cref="IoTErrorCodes"/>）</param>
    /// <param name="description">错误描述（可选，存储本地化参数如设备ID）</param>
    /// <returns>失败的IoTResult</returns>
    /// <exception cref="ArgumentNullException">code为空时抛出</exception>
    public static IoTResult Failed([NotNull] string code, string? description = null)
    {
        Check.NotNullOrEmpty(code, nameof(code));

        var error = new IoTError(code, description);
        return Failed(new[] { error });
    }

    /// <summary>
    /// 创建失败结果（多错误数组）
    /// </summary>
    /// <param name="errors">错误数组</param>
    /// <returns>失败的IoTResult{T}</returns>
    public new static IoTResult<T> Failed(params IoTError[] errors)
    {
        var result = new IoTResult<T> { Succeeded = false };
        result.AddErrors(errors);

        return result;
    }
}
