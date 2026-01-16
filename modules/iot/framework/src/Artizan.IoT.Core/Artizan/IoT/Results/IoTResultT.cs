using Artizan.IoT.Errors;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

    #region 构造函数
    /// <summary>
    /// 保护无参构造函数（兼容序列化/子类自定义初始化）
    /// </summary>
    /// <remarks>仅用于序列化/框架反射，业务代码优先使用带参构造</remarks>
    protected IoTResult()
    {
    }

    /// <summary>
    /// 保护构造函数（供子类初始化状态）
    /// </summary>
    /// <param name="succeeded">是否成功</param>
    /// <param name="data">业务数据</param>
    /// <param name="errors">错误列表</param>
    protected IoTResult(bool succeeded, T? data = default, IEnumerable<IoTError>? errors = null)
        : base(succeeded, errors) // 调用父类构造函数初始化Succeeded和_errors
    {
        Data = data; // 初始化泛型业务数据
    }

    /// <summary>
    /// 简化构造函数（仅成功/失败+错误，数据为默认值）
    /// </summary>
    /// <param name="succeeded">是否成功</param>
    /// <param name="errors">错误列表</param>
    protected IoTResult(bool succeeded, IEnumerable<IoTError>? errors = null)
        : this(succeeded, default, errors)
    {

    }

    #endregion

    /// <summary>
    /// 创建成功结果（带数据）
    /// </summary>
    /// <param name="data">业务数据</param>
    /// <returns>成功的IoTResult{T}</returns>
    public new static IoTResult<T> Success(T? data)
    {
        return new IoTResult<T>(true, data);
    }

    /// <summary>
    /// 创建失败结果（带错误）
    /// </summary>
    /// <param name="errors">错误列表</param>
    /// <returns>失败的IoTResult{T}</returns>
    public new static IoTResult<T> Failed(List<IoTError>? errors)
    {
        return new IoTResult<T>(false, errors);
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
    public static IoTResult<T> Failed([NotNull] string code, string? description = null)
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
        return new IoTResult<T>(false, errors); 
    }

    /// <summary>
    /// 合并多个IoTResult，汇总所有错误（去重）
    /// 设计思路：适配多步骤业务校验，批量汇总错误，避免多次判断结果状态
    /// </summary>
    /// <param name="results">多个IoTResult</param>
    /// <returns>汇总后的结果</returns>
    public static IoTResult<T> Combine(params IoTResult<T>[] results)
    {
        return Combine(default(T), results);
    }

    /// <summary>
    /// 合并多个IoTResult，汇总所有错误（去重）
    /// 设计思路：适配多步骤业务校验，批量汇总错误，避免多次判断结果状态
    /// 合并规则：
    /// 1. 一票否决：只要有一个子结果失败（Succeeded=false），合并结果即为失败；
    /// 2. 错误汇总：合并结果的Errors包含所有子结果的错误（自动去重）；
    /// 3. 数据规则：所有子结果成功时，合并结果的Data为defaultData；有失败时Data为null。
    /// </summary>
    /// <param name="defaultData">默认返回的数据（所有结果成功时使用）</param>
    /// <param name="results">多个IoTResult</param>
    /// <returns>汇总后的结果</returns>
    public static IoTResult<T> Combine(T? defaultData, params IoTResult<T>[] results)
    {
        Check.NotNull(results, nameof(results));

        var allErrors = results.Where(r => !r.Succeeded)
                               .SelectMany(r => r.Errors)
                               .Distinct() // 值对象去重，避免重复错误
                               .ToList();

        return allErrors.Any() ? Failed(allErrors) : Success(defaultData);
    }
}
