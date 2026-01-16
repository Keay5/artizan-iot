using Artizan.IoT.Exceptions;
using Artizan.IoT.Mqtt.Localization;
using Artizan.IoT.Results;
using Microsoft.Extensions.Localization;
using System.Runtime.Serialization;
using Volo.Abp;
using Volo.Abp.Localization;

namespace Artizan.IoT.Mqtt.Exceptions;

public class MqttResultException : IoTResultException
{
    public MqttResultException(IoTResult iotResult) : 
        base(iotResult)
    {
    }

    protected MqttResultException(SerializationInfo info, StreamingContext context) : 
        base(info, context)
    {
    }

    // 重写本地化方法：使用 MQTT 专属 Resource
    public override string LocalizeMessage(LocalizationContext context)
    {
        Check.NotNull(context, nameof(context));

        // 关键点：使用模块专属的 IoTMqttResource
        var localizer = context.LocalizerFactory.Create<IoTMqttResource>();

        SetData(localizer);
        // 注意：这里是 IoTResult
        return IoTResult.LocalizeErrors(localizer);
    }
}
