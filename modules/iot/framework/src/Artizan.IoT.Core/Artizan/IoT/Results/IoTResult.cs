using Artizan.IoT.Errors;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Volo.Abp;

namespace Artizan.IoT.Results;


/*
 /// 使用示例：
/// 1）.IoTResult 循环套用, errors 重新汇总，最终返回新的IoTResult:
/// protected virtual async Task<IoTResult> VerifyClientCanConnecteAsync(Device device, ...)
/// {
///    List<iotError>? errors = null;
///    IoTResult ioTResult;
///  
///    ioTResult = DeviceManager.VerifyDeviceEnable(device.IsEnable);
///                                |
///                                |- public virtual IoTResult VerifyDeviceEnable(bool isEnable)
///                                   {
///                                       List<iotError>? errors = null;
///                                       if (!isEnable)
///                                       {
///                                           errors ??= new List<iotError>();
///                                           errors.Add(new iotError()
///                                           {   
///                                               Code = IoTErrorCodes.DeviceDisabled // 错误码
///                                           });
///                                       }
///                                       
///                                       return errors?.Count > 0 ? IoTResult.Failed(errors) : IoTResult.Success;
///                                    }
///     ......
///     
///     // errors 重新汇总
///     if (ioTResult.Errors.Any()) 
///     {
///        errors ??= [];
///        errors.AddRange(ioTResult.Errors);
///     }
///     
///     iotResult = DeviceManager.VerifyDevicePassword(productKey, deviceName, device.DeviceSecret, password, timestamp);
///                               |
///                               |- public virtual IoTResult VerifyDevicePassword(string productKey, string deviceName, string deviceSecret, string password, long timestamp)
///                                 {
///                                     List<IoTError>? errors = null;
///                                     var mqttSign = new MqttSign();
///                                     
///                                     mqttSign.Calculate(productKey, deviceName, deviceSecret, timestamp);
///                             
///                                     var parsedPassword = mqttSign.Password;
///                                     if (parsedPassword != password)
///                                     {
///                                         errors ??= new List<IoTError>();
///                                         errors.Add(new IoTError()
///                                         {
///                                             Code = IoTErrorCodes.AuthenticationFailed,
///                                             // IoTError错误的本地化描述
///                                             Description = Localizer[
///                                                             IoTErrorCodes.DeviceAuthenticationFailed,
///                                                             deviceName
///                                                           ]
///                                          });
///                                       }
///                             
///                                      return errors?.Count > 0 ? MqttResult.Failed(errors) : MqttResult.Success;
///                                   }
///     // errors 重新汇总
///     if (iotResult.Errors.Any()) 
///     {
///         errors ??= [];
///         errors.AddRange(iotResult.Errors);
///     }
///     
///     // errors 重新汇总，最终返回新的IoTResult
///     return errors?.Count > 0 ? IoTResult.Failed(errors) : IoTResult.Success;
///   }
///   
/// 2）.其它地方调用:
/// 
///    var ioTResult = await VerifyClientCanConnecteAsync(device, ...);
///    
///    ioTResult.CheckErrors(); // 失败则抛出 IoTResultException 异常
///    或者
///    if (!ioTResult.Succeeded)
///    {
///         .....
///    }
///    
/// 使用
/// 定义参考：
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/IdentityResult.cs
/// 使用参考：
/// UserValidator:
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/UserValidator.cs#L17
/// UserManager.cs->ValidateUserAsync()
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/UserManager.cs#L2299
 */

/// <summary>
/// IoT业务操作结果封装类
/// 设计模式：单例模式（成功结果）+ 工厂模式（失败结果）
/// 设计思路：
/// 1. 模仿ASP.NET Core IdentityResult，统一封装操作结果（成功/失败）
/// 2. 成功结果单例化，避免重复创建对象；失败结果通过工厂方法动态创建
/// 3. 仅负责结果封装，无日志、异常、本地化等附加逻辑（单一职责原则）
/// 设计考量：
/// - 不可变性：_errors私有只读，对外暴露IEnumerable{IoTError}，避免外部篡改
/// - 多重载工厂方法：适配单错误、多错误、列表错误等场景，提升易用性
/// - Combine方法：支持多步骤校验的错误汇总（如设备启用→密码校验→权限校验）
/// - 防御性编程：过滤空错误，避免NullReferenceException
/// - 无外部依赖：不依赖日志、本地化等组件，保证纯净性与复用性
/// - 序列化支持：标记DataContract，适配分布式/跨进程场景
/// </summary>
[Serializable] // 支持序列化，适配分布式/跨进程场景
[DataContract] // 数据契约，自动序列化核心属性
public class IoTResult
{
    #region 静态成员（单例+工厂）
    /// <summary>
    /// 成功结果单例（静态只读，全局复用）
    /// 设计思路：单例模式，避免频繁创建相同的成功结果对象，提升性能
    /// </summary>
    private static readonly IoTResult _success = new IoTResult { Succeeded = true };

    /// <summary>
    /// 获取成功结果实例（单例）
    /// </summary>
    public static IoTResult Success => _success;

    /// <summary>
    /// 创建失败结果（单错误）
    /// </summary>
    /// <param name="error">单个错误</param>
    /// <returns>失败的IoTResult</returns>
    public static IoTResult Failed(IoTError error)
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
        // 核心：强制校验错误码非空，避免无标识的失败结果
        Check.NotNullOrEmpty(code, nameof(code));

        var error = new IoTError(code, description);
        return Failed(new[] { error });
    }

    /// <summary>
    /// 创建失败结果（多错误数组）
    /// </summary>
    /// <param name="errors">错误数组</param>
    /// <returns>失败的IoTResult</returns>
    public static IoTResult Failed(params IoTError[] errors)
    {
        var result = new IoTResult { Succeeded = false };
        if (errors != null && errors.Length > 0)
        {
            // 过滤空错误，保证错误列表的纯净性
            result._errors.AddRange(errors.Where(e => e != null));
        }
        return result;
    }

    /// <summary>
    /// 创建失败结果（错误列表）
    /// </summary>
    /// <param name="errors">错误列表</param>
    /// <returns>失败的IoTResult</returns>
    public static IoTResult Failed(List<IoTError>? errors)
    {
        var result = new IoTResult { Succeeded = false };
        if (errors != null && errors.Any())
        {
            result._errors.AddRange(errors.Where(e => e != null));
        }
        return result;
    }

    /// <summary>
    /// 合并多个IoTResult，汇总所有错误（去重）
    /// 设计思路：适配多步骤业务校验，批量汇总错误，避免多次判断结果状态
    /// </summary>
    /// <param name="results">多个IoTResult</param>
    /// <returns>汇总后的结果</returns>
    public static IoTResult Combine(params IoTResult[] results)
    {
        Check.NotNull(results, nameof(results));

        var allErrors = results.Where(r => !r.Succeeded)
                               .SelectMany(r => r.Errors)
                               .Distinct() // 值对象去重，避免重复错误
                               .ToList();

        return allErrors.Any() ? Failed(allErrors) : Success;
    }
    #endregion

    protected IoTResult()
    {
    }

    /// <summary>
    /// 保护构造函数（供子类初始化）
    /// </summary>
    /// <param name="succeeded">是否成功</param>
    /// <param name="errors">错误列表（可选）</param>
    protected IoTResult(bool succeeded, IEnumerable<IoTError>? errors = null)
    {
        Succeeded = succeeded;
        AddErrors(errors);
    }

    #region 实例成员
    /// <summary>
    /// 私有错误列表（保证外部不可直接修改）
    /// </summary>
    protected readonly List<IoTError> _errors = new List<IoTError>();

    /// <summary>
    /// 操作是否成功
    /// [DataMember]：标记为可序列化属性
    /// </summary>
    [DataMember]
    public bool Succeeded { get; protected set; }

    /// <summary>
    /// 错误信息集合（对外只读，IEnumerable不支持增删）
    /// [DataMember]：自动序列化该属性，无需手动处理
    /// </summary>
    [DataMember]
    public IEnumerable<IoTError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// 保护方法：添加错误（统一过滤空错误，保证逻辑一致）
    /// 设计思路：
    /// 1. 封装错误添加逻辑，避免子类重复写过滤代码
    /// 2. 仅允许添加非空错误，保证_errors列表纯净性
    /// </summary>
    /// <param name="errors">待添加的错误列表</param>
    protected void AddErrors(IEnumerable<IoTError>? errors)
    {
        if (errors != null)
        {
            _errors.AddRange(errors.Where(e => e != null));
        }
    }

    /// <summary>
    /// 重写ToString，便于日志/调试输出（仅格式化，不记录）
    /// </summary>
    /// <returns>结果状态+错误码（失败）/Succeeded（成功）</returns>
    public override string ToString()
    {
        if (!Succeeded)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Failed : {0}", string.Join(",", Errors.Select(x => x.Code).ToList()));
        }
        return "Succeeded";
    }
    #endregion


}
