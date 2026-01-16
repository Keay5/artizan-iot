namespace Artizan.IoT.Mqtt.Auths;

/// <summary>
/// MQTT认证错误码
/// </summary>
public enum MqttAuthErrorCode
{
    /// <summary>
    /// 认证成功
    /// </summary>
    Success = 0,

    /// <summary>
    /// 未授权（通用）
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// 权限禁止（通用）
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// 请求参数错误（通用）
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// 服务器内部错误
    /// </summary>
    ServerError = 500,

    #region 业务专属错误码（从600开始，避免与通用HTTP错误码冲突）
    /// <summary>
    /// ClientId格式非法
    /// </summary>
    ClientIdFormatInvalid = 601,

    /// <summary>
    /// 认证类型不匹配
    /// </summary>
    AuthTypeMismatch = 602,

    /// <summary>
    /// 安全模式不匹配
    /// </summary>
    SecurityModeMismatch = 603,

    /// <summary>
    /// 签名验证失败
    /// </summary>
    SignatureVerifyFailed = 604,
    /// <summary>
    /// 认证参数无效
    /// </summary>
    AuthParamsInvalid = 605,
    /// <summary>
    /// 不支持的认证类型
    /// </summary>
    AuthTypeNotSupported = 606,

    #endregion
}
