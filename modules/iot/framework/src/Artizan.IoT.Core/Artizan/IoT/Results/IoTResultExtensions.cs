using Artizan.IoT.Errors;
using Artizan.IoT.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Text.Formatting;

namespace Artizan.IoT.Results;

/// <summary>
/// IoTResult扩展方法类
/// 设计模式：扩展方法模式（Extension Method）
/// 设计思路：
/// 1. 不修改原类代码，增强IoTResult/IoTError的功能（本地化、日志、异常抛出）
/// 2. 单一职责：每个方法只做一件事，降低维护成本
/// 3. 遵循“开闭原则”：新增功能无需修改核心类，仅需添加扩展方法
/// 设计考量：
/// - 严格入参校验：所有入参通过Check.NotNull校验，避免空引用异常
/// - 降级处理：本地化失败时返回DefaultError，保证程序稳定性
/// - 日志解耦：日志扩展方法由调用方注入ILogger，符合DI原则
/// - 代码规范：所有if单行语句均加{}并换行，符合团队编码规范
/// </summary>
public static class IoTResultExtensions
{
    #region 异常抛出扩展

    /*-----------------------------------------------------------------------------------------------------------------------------------------------------------
     IoTResult及其子类的 CheckErrors 方法的使用规范：
            返回值为 IoTResult及其子类（比如：MqttAuthResult） 的方法内部不要调用 CheckError方法
     
     核心理由（为什么要该规范）
     1.职责单一原则：
        IoTResult及其子类（比如：MqttAuthResult）的设计初衷是「返回认证结果（包含成功 / 失败信息）」，而非「直接抛出异常」。
        方法内部调用 CheckError 会让方法同时承担「结果返回」和「异常抛出」两个职责，违背单一职责。
     
    2.调用方灵活性：
        不同调用场景对失败的处理逻辑不同（如：有的场景需要捕获异常，有的仅需判断布尔值）。
        返回 IoTResult及其子类（比如：MqttAuthResult）让调用方自主决定是否 CheckError（抛出异常），而非方法内部强制抛出。
    
    3.代码可读性 / 可维护性：
        方法签名为 IoTResult及其子类（比如：MqttAuthResult）时，开发者预期「该方法返回结果，不抛异常」；若内部隐式抛异常，会破坏预期，增加调试成本。
     *-----------------------------------------------------------------------------------------------------------------------------------------------------------*/

    /// <summary>
    /// 检查 IoTResult 是否成功，失败则抛出 IoTResultException
    /// 设计思路：简化业务代码，一行代码完成「结果校验+异常抛出」
    /// 核心规范：
    /// 1. ✅ 允许场景：返回值为 void/非 IoTResult 类型的方法内部调用（如 GenerateMqttConnectParams）
    /// 2. ❌ 禁止场景：返回值为 MqttAuthResult/IoTResult 类型的方法内部调用（需由调用方决定是否抛出）
    /// 3. 设计原则：结果返回类方法（如 VerifyMqttSign）仅返回结果，异常抛出由调用方自主控制
    /// </summary>
    /// <param name="iotResult">IoT操作结果（不可为null）</param>
    /// <exception cref="ArgumentNullException">iotResult 为空时抛出</exception>
    /// <exception cref="ArgumentException">Errors 集合为空但操作失败时抛出</exception>
    /// <exception cref="IoTResultException">操作失败（Succeeded=false）时抛出</exception>
    /// <remarks>
    /// 使用示例：
    /// ✅ 正确用法（非结果返回方法）：
    /// public MqttConnectParams GenerateMqttConnectParams(...)
    /// {
    ///     var validateResult = ValidateParams(...);
    ///     validateResult.CheckErrors(); // 允许：返回值为MqttConnectParams，非结果类型
    ///     // ... 业务逻辑
    /// }
    /// 
    /// ❌ 错误用法（结果返回方法）：
    /// public MqttAuthResult VerifyMqttSign(...)
    /// {
    ///     var validateResult = ValidateParams(...);
    ///     validateResult.CheckErrors(); // 禁止：返回值为MqttAuthResult，需由调用方决定
    ///     return validateResult;
    /// }
    /// 
    /// ✅ 正确用法（调用方控制）：
    /// var authResult = signer.VerifyMqttSign(...);
    /// authResult.CheckErrors(); // 调用方自主决定是否抛出异常
    /// </remarks>
    public static void CheckErrors([NotNull] this IoTResult iotResult)
    {
        Check.NotNull(iotResult, nameof(iotResult));

        if (iotResult.Succeeded)
        {
            return;
        }

        if (iotResult.Errors == null || !iotResult.Errors.Any())
        {
            throw new ArgumentException("Failed IoTResult must contain at least one error..", nameof(iotResult));
        }

        throw new IoTResultException(iotResult);
    }

    /// <summary>
    /// 重载：失败时抛出自定义异常（需继承IoTResultException，且有接收IoTResult的构造函数）
    /// 设计目的：自动关联IoTResult到自定义异常，无需手动new异常
    /// 设计思路：简化「自定义异常+结果关联」的代码，一行完成校验+异常抛出
    /// 核心规范：
    /// 1. ✅ 允许场景：返回值为 void/非 IoTResult/MqttAuthResult 类型的方法内部调用（如 GenerateMqttConnectParams）
    /// 2. ❌ 禁止场景：返回值为 MqttAuthResult/IoTResult 类型的方法内部调用（需由调用方决定是否抛出）
    /// 3. 设计原则：结果返回类方法（如 VerifyMqttSign）仅返回结果，异常抛出由调用方自主控制
    /// </summary>
    /// <typeparam name="TException">自定义异常类型（必须继承IoTResultException，且包含接收IoTResult的构造函数）</typeparam>
    /// <param name="iotResult">IoT操作结果（不可为null）</param>
    /// <exception cref="ArgumentNullException">iotResult 为空时抛出</exception>
    /// <exception cref="ArgumentException">操作失败但Errors集合为空时抛出</exception>
    /// <exception cref="InvalidOperationException">自定义异常实例化失败（如无匹配构造函数）时抛出</exception>
    /// <exception cref="TException">操作失败（Succeeded=false）时抛出指定的自定义异常</exception>
    /// <remarks>
    /// 使用约束：
    /// 1. 自定义异常必须满足：继承 IoTResultException + 包含构造函数 `public 自定义异常(IoTResult result)`
    /// 2. 调用禁忌：返回 MqttAuthResult 的方法（如 VerifyMqttSign）内部禁止调用，需由调用方执行
    /// 
    /// 使用示例：
    /// ✅ 正确用法（非结果返回方法）：
    /// public MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret)
    /// {
    ///     MqttSignHelper.ValidateProductKey(signParams.ProductKey).CheckError<MqttResultException>(); // 允许：返回值为MqttConnectParams
    ///     // ... 其他业务逻辑
    ///     return new MqttConnectParams();
    /// }
    /// 
    /// ❌ 错误用法（结果返回方法）：
    /// public MqttAuthResult VerifyMqttSign(MqttConnectParams connectParams, string secret)
    /// {
    ///     var parseResult = ParseMqttSignParams(connectParams);
    ///     parseResult.CheckError<MqttResultException>(); // 禁止：返回值为MqttAuthResult，需调用方决定
    ///     return parseResult;
    /// }
    /// 
    /// ✅ 正确用法（调用方控制）：
    /// var authResult = signer.VerifyMqttSign(connectParams, secret);
    /// authResult.CheckError<MqttResultException>(); // 调用方自主决定是否抛出异常
    /// </remarks>
    public static void CheckError<TException>([NotNull] this IoTResult iotResult)
        where TException : IoTResultException // 移除new()约束，放宽灵活性
    {
        // 1. 基础空校验（必加，避免空引用）
        Check.NotNull(iotResult, nameof(iotResult));

        // 2. 成功则直接返回，不抛异常
        if (iotResult.Succeeded)
        {
            return;
        }

        // 3. 强化校验：失败结果必须有错误信息
        if (iotResult.Errors == null || !iotResult.Errors.Any())
        {
            throw new ArgumentException("Failed IoTResult must contain at least one error.", nameof(iotResult));
        }

        // 4. 核心：创建自定义异常并关联IoTResult（修复??运算符问题）
        TException? exception = null;
        try
        {
            // 优先使用「接收IoTResult的构造函数」（自定义异常必须实现此构造）
            exception = (TException)Activator.CreateInstance(
                typeof(TException),
                new object[] { iotResult } // 把失败的IoTResult传给异常
            )!;
        }
        catch (MissingMethodException)
        {
            // 友好提示：告诉用户自定义异常需要什么构造函数
            throw new InvalidOperationException(
                $"Custom exception {typeof(TException).Name} must have a constructor with parameter type {nameof(IoTResult)}.",
                innerException: null
            );
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is TargetInvocationException)
        {
            // 捕获其他实例化失败的异常，统一提示
            throw new InvalidOperationException(
                $"Failed to create instance of custom exception {typeof(TException).Name}: {ex.Message}",
                innerException: ex
            );
        }

        // 5. 修复：先判断exception是否为null，再抛异常（替代??）
        if (exception == null)
        {
            throw new InvalidOperationException(
                $"Failed to create instance of custom exception {typeof(TException).Name}: instance is null."
            );
        }

        // 6. 抛出关联了IoTResult的自定义异常
        throw exception;
    }

    #endregion

    #region 本地化扩展
    /// <summary>
    /// 从错误描述中提取本地化参数（适配模板化本地化）
    /// 设计思路：如模板"设备{0}已禁用"，从Description中提取"Sensor001"作为参数
    /// </summary>
    /// <param name="iotResult">失败的IoTResult</param>
    /// <param name="localizer">本地化器</param>
    /// <returns>参数数组</returns>
    /// <exception cref="ArgumentNullException">入参为空时抛出</exception>
    /// <exception cref="ArgumentException">成功结果时抛出</exception>
    public static string[] GetValuesFromErrorMessage([NotNull] this IoTResult iotResult, [NotNull] IStringLocalizer localizer)
    {
        Check.NotNull(iotResult, nameof(iotResult));
        Check.NotNull(localizer, nameof(localizer));

        if (iotResult.Succeeded)
        {
            throw new ArgumentException("iotResult.Succeeded should be false to get values from error.", nameof(iotResult));
        }

        if (iotResult.Errors == null || !iotResult.Errors.Any())
        {
            throw new ArgumentException("iotResult.Errors should not be null or empty.", nameof(iotResult));
        }

        var allValues = new List<string>();
        foreach (var error in iotResult.Errors)
        {
            var values = error.ExtractLocalizationParameters(localizer);
            if (values.Any())
            {
                allValues.AddRange(values);
            }
        }

        return allValues.ToArray();
    }

    /// <summary>
    /// 本地化IoTResult中的所有错误信息
    /// 设计思路：批量处理错误本地化，拼接成可读字符串，降低业务层代码复杂度
    /// </summary>
    /// <param name="iotResult">失败的IoTResult</param>
    /// <param name="localizer">本地化器</param>
    /// <returns>拼接后的本地化错误信息</returns>
    /// <exception cref="ArgumentNullException">入参为空时抛出</exception>
    /// <exception cref="ArgumentException">成功结果时抛出</exception>
    public static string LocalizeErrors([NotNull] this IoTResult iotResult, [NotNull] IStringLocalizer localizer)
    {
        Check.NotNull(iotResult, nameof(iotResult));
        Check.NotNull(localizer, nameof(localizer));

        if (iotResult.Succeeded)
        {
            throw new ArgumentException("iotResult.Succeeded should be false to localize errors.", nameof(iotResult));
        }

        if (iotResult.Errors == null || !iotResult.Errors.Any())
        {
            throw new ArgumentException("iotResult.Errors should not be null or empty.", nameof(iotResult));
        }

        return iotResult.Errors
            .Select(err => err.LocalizeErrorMessage(localizer))
            .Where(msg => !string.IsNullOrEmpty(msg)) // 过滤空消息，避免无效拼接
            .JoinAsString(", ");
    }

    /// <summary>
    /// 本地化单个IoTError的错误信息（支持模板参数替换）
    /// 设计思路：
    /// 1. 优先按错误码读取本地化模板（如IoT_DeviceDisabled → "设备{0}已禁用"）
    /// 2. 提取Description中的参数替换模板占位符
    /// 3. 本地化失败时返回DefaultError，保证程序不崩溃
    /// </summary>
    /// <param name="error">IoT错误信息</param>
    /// <param name="localizer">本地化器</param>
    /// <returns>本地化后的错误信息</returns>
    /// <exception cref="ArgumentNullException">入参为空时抛出</exception>
    public static string LocalizeErrorMessage([NotNull] this IoTError error, [NotNull] IStringLocalizer localizer)
    {
        Check.NotNull(error, nameof(error));
        Check.NotNull(localizer, nameof(localizer));

        // 步骤1：按错误码获取本地化模板
        var localizedString = localizer[error.Code];

        // 步骤2：本地化失败时返回默认错误（降级处理）
        if (localizedString.ResourceNotFound)
        {
            return localizer[IoTErrorCodes.DefaultError];
        }

        // 步骤3：提取参数并替换模板占位符
        var parameters = error.ExtractLocalizationParameters(localizer);
        return parameters.Any()
            ? string.Format(localizedString.Value, parameters.Cast<object>().ToArray())
            : localizedString.Value;
    }

    /// <summary>
    /// 从IoTError的Description中提取本地化参数（私有辅助方法）
    /// 设计思路：利用ABP的FormattedStringValueExtracter解析模板参数，适配多种参数格式
    /// </summary>
    /// <param name="error">IoT错误信息</param>
    /// <param name="localizer">本地化器</param>
    /// <returns>参数数组</returns>
    private static string[] ExtractLocalizationParameters(this IoTError error, IStringLocalizer localizer)
    {
        if (string.IsNullOrEmpty(error.Description))
        {
            return Array.Empty<string>();
        }

        // 示例：模板="Device {0} is disabled"，Description="Device001" → 提取["Device001"]
        if (FormattedStringValueExtracter.IsMatch(error.Description, localizer[error.Code].Value, out var values))
        {
            return values.Select(v => v?.ToString() ?? string.Empty).ToArray();
        }

        // 兼容直接传入参数的场景（如Description="Sensor001"）
        return new[] { error.Description };
    }
    #endregion

    #region 日志扩展（解耦设计，由调用方注入ILogger）
    /// <summary>
    /// 记录IoTResult的日志（失败结果）
    /// 设计思路：将日志逻辑作为扩展方法，核心类无耦合，调用方按需使用
    /// </summary>
    /// <param name="result">IoT操作结果</param>
    /// <param name="logger">日志实例（由调用方注入）</param>
    /// <param name="businessContext">业务上下文描述（如"设备启用"）</param>
    public static void LogIfFailed(this IoTResult result, ILogger logger, string businessContext)
    {
        Check.NotNull(result, nameof(result));
        Check.NotNull(logger, nameof(logger));
        Check.NotNullOrEmpty(businessContext, nameof(businessContext));

        if (result.Succeeded)
        {
            return;
        }

        logger.LogWarning(
            "[{BusinessContext}] IoTResult is Failed | ErrorCodes: {ErrorCodes} | ErrorDetails: {ErrorDetails}",
            businessContext,
            result.Errors.Select(e => e.Code).JoinAsString(","),
            result.Errors.Select(e => e.ToString()).JoinAsString("; ")
        );
    }

    /// <summary>
    /// 记录IoTResult日志（带自定义参数，泛型ILogger）
    /// </summary>
    /// <typeparam name="T">日志分类类型</typeparam>
    /// <param name="result">IoT操作结果</param>
    /// <param name="logger">日志实例</param>
    /// <param name="businessContext">业务上下文（支持格式化，如"设备{0}启用"）</param>
    /// <param name="args">自定义参数（如设备ID）</param>
    public static void LogIfFailed<T>(this IoTResult result, ILogger<T> logger, string businessContext, params object[] args)
    {
        Check.NotNull(result, nameof(result));
        Check.NotNull(logger, nameof(logger));
        Check.NotNullOrEmpty(businessContext, nameof(businessContext));

        if (result.Succeeded)
        {
            return;
        }

        var customContext = string.Format(businessContext, args);
        logger.LogWarning(
            "[{CustomContext}] IoTResult is Failed | ErrorCodes: {ErrorCodes}",
            customContext,
            result.Errors.Select(e => e.Code).JoinAsString(",")
        );
    }
    #endregion
}
