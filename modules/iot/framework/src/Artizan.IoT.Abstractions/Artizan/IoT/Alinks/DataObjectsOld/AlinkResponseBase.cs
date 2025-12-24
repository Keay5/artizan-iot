using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

/// <summary>
/// Alink协议通用响应基类（所有云端响应均继承此类）
/// </summary>
public class AlinkResponseBase
{
    /// <summary>
    /// 消息ID号。String类型的数字，取值范围0~4294967295，且每个消息ID在当前设备中具有唯一性。
    /// 消息ID（与请求ID一致，设备端需保证当日唯一）
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 响应码（200=成功，其他为错误码，参考阿里云IoT错误码文档）
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应描述（成功为"success"，失败为具体错误信息）
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "success";

    /// <summary>
    /// 协议版本（固定为1.0）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 请求方法
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; }

    /// <summary>
    /// 响应数据（部分场景返回业务数据，无则为空）
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
