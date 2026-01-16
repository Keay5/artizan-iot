using Artizan.IoT.Errors;
using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Results;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace Artizan.IoT.Mqtt.Auth;

/// <summary>
/// MQTT认证结果（仅语义化别名，无额外属性，继承非泛型IoTResult）
/// 设计思路：
/// 1. 纯语义化子类，仅复用父类IoTResult的“成功/失败/错误列表”核心逻辑；
/// 2. 静态方法仅做转发，无重复代码，保证逻辑统一；
/// 3. 调用体验与父类一致，但语义更贴合MQTT认证场景。
/// </summary>
/// <summary>
/// MQTT认证结果（纯语义化子类，无额外属性）
/// </summary>
public class MqttAuthResult : IoTResult<MqttSignParams>
{
    #region 私有构造函数（调用父类protected构造函数）
    /// <summary>
    /// 私有构造函数（仅子类内部使用）
    /// </summary>
    private MqttAuthResult(bool succeeded, IEnumerable<IoTError>? errors = null)
        : base(succeeded, errors) // 调用父类构造函数，合法初始化Succeeded和_errors
    {
    }

    /// <summary>
    /// 私有构造函数（带业务数据，适配Success(authParams)场景）
    /// </summary>
    /// <param name="succeeded">是否成功</param>
    /// <param name="signParams">MQTT 签名参数（业务数据）</param>
    /// <param name="errors">错误列表</param>
    private MqttAuthResult(bool succeeded, MqttSignParams? signParams, IEnumerable<IoTError>? errors = null)
        : base(succeeded, signParams, errors) // 调用父类带数据的构造函数
    {
    }

    #endregion

    #region 静态工厂方法
    /// <summary>
    /// 创建MQTT认证成功结果
    /// </summary>
    public static new MqttAuthResult Success()
    {
        // 复用父类成功结果的错误列表（空）
        return new MqttAuthResult(true, IoTResult.Success.Errors);
    }

    /// <summary>
   /// 创建MQTT认证成功结果（带业务数据）
   /// </summary>
   /// <param name="signParams">MQTT签名参数</param>
   /// <returns>MqttAuthResult</returns>
   public static new MqttAuthResult Success(MqttSignParams? signParams)
   {
       return new MqttAuthResult(true, signParams, Enumerable.Empty<IoTError>());
   }

    /// <summary>
    /// 创建MQTT认证失败结果（单错误）
    /// </summary>
    public static new MqttAuthResult Failed([NotNull] string errorCode, string? description = null)
    {
        Check.NotNullOrEmpty(errorCode, nameof(errorCode));
        // 先创建父类失败结果，再提取错误列表传给子类构造函数
        var parentFailed = IoTResult.Failed(errorCode, description);
        return new MqttAuthResult(false, parentFailed.Errors);
    }

    /// <summary>
    /// 创建MQTT认证失败结果（多错误）
    /// </summary>
    public static new MqttAuthResult Failed(params IoTError[] errors)
    {
        var parentFailed = IoTResult.Failed(errors);
        return new MqttAuthResult(false, parentFailed.Errors);
    }

    /// <summary>
    /// 合并多个MQTT认证结果
    /// </summary>
    public static MqttAuthResult Combine(params MqttAuthResult[] results)
    {
        return Combine(default(MqttSignParams), results);
    }

    /// <summary>
    /// 合并多个MQTT认证结果（支持指定成功时的默认数据）
    /// 合并规则：
    /// 1. 一票否决：只要有一个子结果失败（Succeeded=false），合并结果即为失败；
    /// 2. 错误汇总：合并结果的Errors包含所有子结果的错误（自动去重）；
    /// 3. 数据规则：所有子结果成功时，合并结果的Data为defaultData；有失败时Data为null。
    /// </summary>
    /// <param name="defaultData">所有结果成功时返回的默认数据</param>
    /// <param name="results">待合并的MQTT认证结果（不可为null，且至少包含一个结果）</param>
    /// <returns>合并后的MQTT认证结果</returns>
    /// <exception cref="ArgumentNullException">results为null时抛出</exception>
    /// <exception cref="ArgumentException">results为空数组时抛出</exception>
    public static MqttAuthResult Combine(MqttSignParams? defaultData, params MqttAuthResult[] results)
    {
        Check.NotNull(results, nameof(results));
        //if (results.Length == 0)
        //{
        //    throw new ArgumentException("待合并的MQTT认证结果不能为空数组", nameof(results));
        //}
        var combined = IoTResult<MqttSignParams>.Combine(defaultData, results);

        return combined.Errors.Any()
             ? Failed(combined.Errors.ToArray())
             : Success(combined.Data);
    }

    #endregion
}