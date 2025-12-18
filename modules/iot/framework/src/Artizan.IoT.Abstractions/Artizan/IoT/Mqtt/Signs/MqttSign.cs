using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtt.Signs;
/// <summary>
/// MQTT签名
/// MQTT-TLS连接通信
/// https://help.aliyun.com/zh/iot/user-guide/establish-mqtt-connections-over-tcp?spm=a2c4g.11186623.0.0.3139283cCc2j31
/// </summary>
public class MqttSign
{
    public virtual string ClientId { get; protected set; }
    public virtual string UserName { get; protected set; }
    public virtual string Password { get; protected set; }

    /// <summary>
    /// 生成 MQTT签名参数
    /// 参见：
    /// MQTT 客户端直连：https://help.aliyun.com/zh/iot/user-guide/establish-mqtt-connections-over-tcp?spm=a2c4g.11186623.0.0.3c3e2388cZfRFo
    /// Paho-MQTT C#接入示例: https://help.aliyun.com/zh/iot/use-cases/use-the-paho-mqtt-csharp-client?spm=a2c4g.11186623.0.i1#task-2360906
    /// 
    /// 如何使用？
    /// MQTT Client的 Client ID设置为如下格式:
    ///     myProductKey.myDeviceName|securemode=2,signmethod=hmacsha256,timestamp=   
    /// 例如：
    ///     adcbfda13ed5707e726a3a124c567292.device-t1|securemode=2,signmethod=hmacsha256,timestamp=
    ///  
    /// 设备秘钥 deviceSecret
    ///     044d128f901d884b07a03a12bbdb3973cc20f244112deb32a8c73a12bbdb3973
    /// 
    /// 使用 Client ID （作为 plainPassword 明文）和 deviceSecret（作为key）传入算法函数 HmacSha256(...)得到的密文：
    ///     3DF89695A2DCF12B1EE8C04A433CE355F78AC76D684944D0A8A6E07EFAF76869
    /// 以此密文作为 MQTT Client的 Password。
    /// 
    /// 然后将密文保存在设备中，设备使用 MQTT 协议连接时，需提供如下两个参数：
    ///     - MQTT Client的 Client ID:adcbfda13ed5707e726a3a124c567292.device-t1|securemode=2,signmethod=hmacsha256,timestamp=
    ///     - 密文(MQTT Client的 Password):3DF89695A2DCF12B1EE8C04A433CE355F78AC76D684944D0A8A6E07EFAF76869
    /// 
    /// 服务端收到连接请求后，会使用相同的算法和参数计算出另一段密文，与设备上传的 MQTT Client的 Password（密文） 进行对比。
    ///     - 相同，签名验证通过，允许连接。
    ///     - 不同，签名验证失败，拒绝连接。
    ///     
    /// </summary>
    /// <param name="productKey">ProductKey</param>
    /// <param name="deviceName">DeviceName</param>
    /// <param name="deviceSecret">DeviceSecret</param>
    /// <param name="signmethod">签名算法类型，参见 <see cref="MqttSignMethods"/></param>
    /// <param name="timestamp">
    /// 时间戳，生成代码：
    ///     var timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
    /// </param>
    /// <returns></returns>

    public void Calculate(string productKey, 
        [NotNull]string deviceName,
        [NotNull] string deviceSecret,
        [NotNull] string signmethod = MqttSignMethods.HmacSha256, 
        string? timestamp = null)
    {
        //MQTT ClientId
        ClientId = $"{productKey}.{deviceName}|securemode=2,signmethod={signmethod},timestamp={timestamp ?? ""}|";

        //MQTT用户名
        UserName = $"{deviceName}&{productKey}";

        //原始密码，其组成部分如下：
        //提交给服务器的参数（productKey、deviceName、timestamp和clientId），
        //按照参数名称首字母字典排序， 然后将参数值依次拼接。
        var plainPassword = $"clientId{productKey}.{deviceName}deviceName{deviceName}productKey{productKey}timestamp{timestamp ?? ""}";

        //MQTT密码（原始密码加密后的密码）
        if (signmethod.ToLower() == MqttSignMethods.HmacSha256)
        {
            Password = MqttSignCrypto.HmacSha256(plainPassword, deviceSecret);
        }
        else
        {
            throw new NotSupportedException($"The MQTT sign method '{signmethod}' is not supported.");   
        }
    }
}

