using Artizan.IoT.Alinks.DataObjects.Commons;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjects.OmMonitoring;

/// <summary>
/// 远程登录请求（云端→设备，发起远程登录指令）
/// 【协议规范】：https://help.aliyun.com/zh/iot/user-guide/remote-logon
/// Method：thing.service.remote.login
/// Topic模板：/sys/${productKey}/${deviceName}/thing/service/remote/login
/// </summary>
public class RemoteLoginRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.service.remote.login";

    [JsonPropertyName("params")]
    public RemoteLoginParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/service/remote/login";
    }

    /// <summary>
    /// 校验登录参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.SessionId))
        {
            return ValidateResult.Failed("会话ID（SessionId）不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.Protocol))
        {
            return ValidateResult.Failed("登录协议（Protocol）不能为空");
        }
        if (Params.Timeout <= 0)
        {
            return ValidateResult.Failed("会话超时时间（Timeout）必须大于0");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 远程登录参数
/// </summary>
public class RemoteLoginParams
{
    /// <summary>
    /// 远程登录会话ID（平台生成，用于唯一标识）
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 登录协议（ssh/telnet/vnc/rdp）
    /// </summary>
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "ssh";

    /// <summary>
    /// 登录端口（如ssh默认22）
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 22;

    /// <summary>
    /// 登录凭证（加密后的用户名+密码/密钥）
    /// </summary>
    [JsonPropertyName("credential")]
    public string Credential { get; set; } = string.Empty;

    /// <summary>
    /// 凭证加密算法（aes256/rsa）
    /// </summary>
    [JsonPropertyName("encryptMethod")]
    public string EncryptMethod { get; set; } = "aes256";

    /// <summary>
    /// 会话超时时间（秒）
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 3600;

    /// <summary>
    /// 是否允许交互式操作
    /// </summary>
    [JsonPropertyName("interactive")]
    public bool Interactive { get; set; } = true;

    /// <summary>
    /// 登录后执行的指令（可选，如"ls -l"）
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// 远程登录响应（设备→云端，返回登录结果）
/// </summary>
public class RemoteLoginResponse : AlinkResponseBase<RemoteLoginResponseData>
{
}

/// <summary>
/// 远程登录响应数据
/// </summary>
public class RemoteLoginResponseData
{
    /// <summary>
    /// 远程登录会话ID（与请求一致）
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 登录结果（success/failed）
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因（成功时为空）
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 远程访问地址（如设备内网IP:端口）
    /// </summary>
    [JsonPropertyName("accessAddress")]
    public string AccessAddress { get; set; } = string.Empty;

    /// <summary>
    /// 会话令牌（用于后续操作鉴权）
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 令牌过期时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("tokenExpireTime")]
    public long TokenExpireTime { get; set; } = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds();
}

/// <summary>
/// 远程登录状态上报请求（设备→云端，上报会话状态）
/// Method：thing.event.remote.login.status.post
/// Topic模板：/sys/${productKey}/${deviceName}/thing/event/remote.login.status.post
/// </summary>
public class RemoteLoginStatusPostRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.event.remote.login.status.post";

    [JsonPropertyName("params")]
    public RemoteLoginStatusParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/event/remote.login.status.post";
    }

    /// <summary>
    /// 校验状态参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.SessionId))
        {
            return ValidateResult.Failed("会话ID（SessionId）不能为空");
        }
        var validStatus = new List<string> { "connected", "disconnected", "timeout", "error" };
        if (!validStatus.Contains(Params.Status.ToLower()))
        {
            return ValidateResult.Failed($"会话状态（Status）必须是：{string.Join("/", validStatus)}");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 远程登录状态参数
/// </summary>
public class RemoteLoginStatusParams
{
    /// <summary>
    /// 远程登录会话ID
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 会话状态（connected=已连接/disconnected=已断开/timeout=超时/error=异常）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 状态描述（异常时填写原因）
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 会话持续时长（秒）
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 0;

    /// <summary>
    /// 状态上报时间戳（UTC毫秒级）
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// 远程登录状态上报响应
/// </summary>
public class RemoteLoginStatusPostResponse : AlinkResponseBase
{
}

/// <summary>
/// 远程登出请求（云端→设备，终止远程登录会话）
/// Method：thing.service.remote.logout
/// Topic模板：/sys/${productKey}/${deviceName}/thing/service/remote/logout
/// </summary>
public class RemoteLogoutRequest : AlinkRequestBase
{
    [JsonPropertyName("method")]
    public override string Method => "thing.service.remote.logout";

    [JsonPropertyName("params")]
    public RemoteLogoutParams Params { get; set; } = new();

    /// <summary>
    /// 生成Topic
    /// </summary>
    public string GenerateTopic(string productKey, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey), "ProductKey不能为空");
        }
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentNullException(nameof(deviceName), "DeviceName不能为空");
        }
        return $"/sys/{productKey}/{deviceName}/thing/service/remote/logout";
    }

    /// <summary>
    /// 校验登出参数
    /// </summary>
    public ValidateResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Params.SessionId))
        {
            return ValidateResult.Failed("会话ID（SessionId）不能为空");
        }
        if (string.IsNullOrWhiteSpace(Params.Token))
        {
            return ValidateResult.Failed("会话令牌（Token）不能为空");
        }
        return ValidateResult.Success();
    }
}

/// <summary>
/// 远程登出参数
/// </summary>
public class RemoteLogoutParams
{
    /// <summary>
    /// 远程登录会话ID
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 会话令牌（鉴权用）
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 登出原因（如manual=手动登出/timeout=超时/error=异常）
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "manual";
}

