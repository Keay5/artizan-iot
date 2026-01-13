using System;

namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// MQTT连接校验结果
/// </summary>
public class MqttAuthResult
{
    /// <summary>
    /// 是否校验通过
    /// /summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误码（0=成功，参考阿里云IoT错误码）
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 解析出的连接参数（如AuthType、ProductKey等）
    /// </summary>
    public MqttAuthParams? Params { get; set; }

    #region 静态工厂方法（通用实例创建）

    /// <summary>
    /// 创建认证成功的结果
    /// </summary>
    /// <param name="authParams">解析出的连接参数（可选）</param>
    /// <returns>认证成功的MqttAuthResult实例</returns>
    public static MqttAuthResult Success(MqttAuthParams authParams = null)
    {
        return new MqttAuthResult
        {
            IsSuccess = true,
            Code = (int)MqttAuthErrorCode.Success, // 0代表成功
            Message = "认证成功",
            Params = authParams
        };
    }

    /// <summary>
    /// 创建认证失败的结果
    /// </summary>
    /// <param name="errorCode">错误码（非0）</param>
    /// <param name="message">错误信息</param>
    /// <param name="authParams">解析出的连接参数（可选）</param>
    /// <returns>认证失败的MqttAuthResult实例</returns>
    public static MqttAuthResult Fail(MqttAuthErrorCode errorCode, string? message = null, MqttAuthParams authParams = null)
    {
        // 防御性检查：确保错误码不为0（0是成功码）
        if (errorCode == (int)MqttAuthErrorCode.Success)
        {
            throw new ArgumentException("错误码不能为0（0代表成功）", nameof(errorCode));
        }

        // 优先使用自定义信息，无自定义则用默认提示
        message = string.IsNullOrEmpty(message)
            ? GetDefaultMessage(errorCode)
            : message;

        return new MqttAuthResult
        {
            IsSuccess = false,
            Code = (int)errorCode,
            Message = message,
            Params = authParams
        };
    }

    #endregion

    /// <summary>
    /// 根据错误码获取默认提示语
    /// </summary>
    private static string GetDefaultMessage(MqttAuthErrorCode code)
    {
        return code switch
        {
            MqttAuthErrorCode.ClientIdFormatInvalid => "ClientId格式非法",
            MqttAuthErrorCode.AuthTypeMismatch => "认证类型不匹配",
            MqttAuthErrorCode.SecurityModeMismatch => "安全模式不匹配",
            MqttAuthErrorCode.SignatureVerifyFailed => "签名验证失败",
            MqttAuthErrorCode.AuthParamsInvalid => "认证参数无效",
            MqttAuthErrorCode.Unauthorized => "设备未授权",
            MqttAuthErrorCode.Forbidden => "权限不足",
            MqttAuthErrorCode.BadRequest => "请求参数错误",
            MqttAuthErrorCode.ServerError => "服务器内部错误",
            _ => "认证失败"
        };
    }
}

