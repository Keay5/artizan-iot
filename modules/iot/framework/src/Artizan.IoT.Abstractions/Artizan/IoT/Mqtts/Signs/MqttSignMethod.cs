namespace Artizan.IoT.Mqtts.Signs;

/// <summary>
/// 签名算法类型
/// </summary>
public enum MqttSignMethod
{

    /// <summary>
    /// HmacSha1 算法
    /// </summary>
    HmacSha1 = 1,
    /// <summary>
    /// HmacSha256 算法（推荐）
    /// </summary>
    HmacSha256 = 2,
    /// <summary
    /// >HmacMD5 算法
    /// </summary>
    HmacMd5 = 3,
}
