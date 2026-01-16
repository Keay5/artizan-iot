using Artizan.IoT.Localization;
using Artizan.IoT.Results;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Volo.Abp;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Localization;

namespace Artizan.IoT.Exceptions;

/// <summary>
/// 关联IoTResult的业务异常
/// 设计模式：异常封装模式（Exception Wrapping）
/// 设计思路：
/// 1. 继承ABP的BusinessException，融入ABP全局异常处理体系（自动返回标准化错误响应）
/// 2. 封装IoTResult，保留完整的错误上下文，便于问题排查
/// 3. 修复序列化构造函数问题：不再调用不存在的base(info, context)，改为手动赋值属性
/// 设计考量：
/// - 必须继承BusinessException：复用ABP的异常处理、日志、本地化能力，避免重复造轮子
/// - 序列化安全：使用DataContract替代过时的GetObjectData，符合.NET最新规范
/// - 防御性校验：确保关联的IoTResult为失败状态且包含错误信息
/// - 兼容性：保留序列化构造函数，适配分布式/跨进程场景
/// </summary>
[Serializable] // 标记可序列化，兼容.NET序列化体系
[DataContract] // 数据契约，自动序列化核心属性
public class IoTResultException : BusinessException, ILocalizeErrorMessage
{
    /// <summary>
    /// 关联的IoTResult（保留完整错误上下文）
    /// [DataMember]：自动序列化该属性，无需手动处理GetObjectData
    /// </summary>
    [DataMember]
    public IoTResult IoTResult { get; }

    #region 核心构造函数（业务使用）
    /// <summary>
    /// 构造函数（关联失败的IoTResult）
    /// </summary>
    /// <param name="iotResult">失败的IoTResult</param>
    /// <exception cref="ArgumentNullException">IoTResult为空时抛出</exception>
    /// <exception cref="ArgumentException">IoTResult为成功/无错误时抛出</exception>
    public IoTResultException(IoTResult iotResult)
        : base(
              code: iotResult.Errors?.FirstOrDefault()?.Code ?? IoTErrorCodes.DefaultError,
              message: iotResult.Errors?.Select(err => err.Description ?? string.Empty).JoinAsString(", ") ?? string.Empty,
              logLevel: LogLevel.Warning)
    {
        // 防御性校验：IoTResult不能为空
        Check.NotNull(iotResult, nameof(iotResult));

        IoTResult = iotResult;

        // 防御性校验：必须是失败结果
        if (iotResult.Succeeded)
        {
            throw new ArgumentException("IoTResult must be failed to create IoTResultException.", nameof(iotResult));
        }

        // 防御性校验：必须包含错误信息
        if (iotResult.Errors == null || !iotResult.Errors.Any())
        {
            throw new ArgumentException("IoTResult must contain at least one error.", nameof(iotResult));
        }
    }

    #endregion

    #region 序列化构造函数（.NET框架自动调用，核心修正版）
    /// <summary>
    /// 序列化构造函数（核心修正：不再调用base(info, context)）
    /// 设计思路：
    /// 1. 先调用BusinessException的无参构造函数（base()），避免编译错误
    /// 2. 手动从SerializationInfo中读取并赋值所有核心属性：
    ///    - BusinessException的Code/Details/LogLevel
    ///    - 自定义的IoTResult属性
    /// 3. 防御性校验：确保info不为空，避免NullReferenceException
    /// </summary>
    /// <param name="info">序列化信息</param>
    /// <param name="context">序列化上下文</param>
    /// <exception cref="ArgumentNullException">info为空时抛出</exception>
    protected IoTResultException(SerializationInfo info, StreamingContext context)
        : base() // 关键修正：调用BusinessException无参构造函数
    {
        Check.NotNull(info, nameof(info));

        // 步骤1：手动读取BusinessException的核心属性并赋值
        Code = info.GetString(nameof(Code));
        Details = info.GetString(nameof(Details));
        LogLevel = (LogLevel)info.GetValue(nameof(LogLevel), typeof(LogLevel))!;

        // 步骤2：手动读取自定义的IoTResult属性并赋值
        IoTResult = (IoTResult)info.GetValue(nameof(IoTResult), typeof(IoTResult))!;

        // 步骤3：读取Exception基类的核心属性（增强兼容性）
        var message = info.GetString("Message");
        if (!string.IsNullOrEmpty(message))
        {
            // 手动设置Message（如需）
            this.Data["Message"] = message;
        }
    }
    #endregion

    #region 过时的GetObjectData（保留兼容，标记为过时）
    /// <summary>
    /// 实现ISerializable接口的GetObjectData（标记为过时，引导使用数据契约）
    /// 设计思路：
    /// 1. 标记为过时，提示开发者使用DataContract序列化
    /// 2. 保留基础实现，避免旧代码/第三方序列化框架报错
    /// 3. 手动写入所有核心属性，保证序列化完整性
    /// </summary>
    /// <param name="info">序列化信息</param>
    /// <param name="context">序列化上下文</param>
    [Obsolete("This method is obsolete, use DataContract serialization instead. DiagnosticId: LegacyFormatterImplDiagId, Url: https://docs.microsoft.com/en-us/dotnet/standard/serialization/obsolete-serialization-apis")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        Check.NotNull(info, nameof(info));

        // 步骤1：调用基类实现（BusinessException可能已处理部分属性）
        base.GetObjectData(info, context);

        // 步骤2：手动写入BusinessException的核心属性（确保不遗漏）
        info.AddValue(nameof(Code), Code);
        info.AddValue(nameof(Details), Details);
        info.AddValue(nameof(LogLevel), LogLevel);

        // 步骤3：手动写入自定义的IoTResult属性
        info.AddValue(nameof(IoTResult), IoTResult);
    }
    #endregion

    #region 本地化实现
    /// <summary>
    /// 实现ILocalizeErrorMessage，提供本地化异常消息
    /// 设计思路：
    /// 1. 集成ABP本地化框架，支持多语言错误消息
    /// 2. 从IoTResult中提取错误信息并本地化，保证消息的用户友好性
    /// </summary>
    /// <param name="context">本地化上下文</param>
    /// <returns>本地化后的错误消息</returns>
    public virtual string LocalizeMessage(LocalizationContext context)
    {
        Check.NotNull(context, nameof(context));

        // 步骤1：创建IoT模块的本地化器
        var localizer = context.LocalizerFactory.Create<IoTResource>();

        // 步骤2：设置本地化参数到异常Data字典（供ABP全局异常处理器使用）
        SetData(localizer);

        // 步骤3：本地化所有错误信息并返回
        return IoTResult.LocalizeErrors(localizer);
    }

    /// <summary>
    /// 设置本地化参数到异常Data字典
    /// 设计思路：参数存入Data后，ABP全局异常处理器可读取并用于日志/返回结果
    /// </summary>
    /// <param name="localizer">本地化器</param>
    protected virtual void SetData(IStringLocalizer localizer)
    {
        Check.NotNull(localizer, nameof(localizer));

        var values = IoTResult.GetValuesFromErrorMessage(localizer);
        for (var index = 0; index < values.Length; index++)
        {
            Data[index.ToString()] = values[index];
        }
    }
    #endregion
}