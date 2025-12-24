namespace Artizan.IoT.Mqtts.Signs;

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
    public string Message { get; set; }

    /// <summary>
    /// 解析出的连接参数（如AuthType、ProductKey等）
    /// </summary>
    public MqttAuthParams Params { get; set; }
}
