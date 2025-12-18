using Artizan.IoTHub.Mqtts.Servers;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Server.Extensions;

public static class MqttServerExtensions
{
    /// <summary>
    ///  -----------------------------------------------------------------  
    ///  MQTTnet v3.x :
    ///     MQTTnet v3.x 中的MqttServer 类中的方法PublishAsync() 已被删除，故以下代码不能再使用:
    ///
    ///     await MqttServer.PublishAsync(BrokerEventTopics.NewClientConnected, arg.ClientId);
    ///     ，其源码： https://github.com/dotnet/MQTTnet/blob/release/3.1.x/Source/MQTTnet/Server/MqttServer.cs
    ///     
    ///  ----------------------------------------------------------------- 
    ///   MQTTnet v4.1 ：
    ///     该版本中可以调用：MqttServer.InjectApplicationMessage() 方法注入消息
    ///    
    /// </summary>
    /// <param name="mqttServer">MQTTT Server</param>
    /// <param name="topic">MQTTT 主题</param>
    /// <param name="payload">MQTTT 消息载荷</param>
    /// <exception cref="ArgumentNullException"></exception>

    public static async Task PublishByBrokerAsync(this MqttServer mqttServer, string topic, byte[] payload)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));

         await mqttServer.PublishByBrokerAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());
    }

    public static async Task PublishByBrokerAsync(this MqttServer mqttServer, MqttApplicationMessage mqttApplicationMessage)
    {
        if (mqttServer == null) throw new ArgumentNullException(nameof(mqttServer));
        if (mqttApplicationMessage == null) throw new ArgumentNullException(nameof(mqttApplicationMessage));

        await mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(mqttApplicationMessage)
        {
            SenderClientId = MqttServiceConsts.IoTMqttInternalClientId
        });

    }
}
