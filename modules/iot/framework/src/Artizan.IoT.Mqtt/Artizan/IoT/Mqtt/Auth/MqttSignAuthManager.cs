using Artizan.IoT.Mqtt.Auth.Signs;
using Artizan.IoT.Mqtt.Exceptions;
using Artizan.IoT.Results;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Auth;

public class MqttSignAuthManager : IMqttSignAuthManager, ISingletonDependency
{
    protected MqttSignerFactory MqttSignerFactory { get; }
    public MqttSignAuthManager(MqttSignerFactory mqttSignerFactory)
    {
        MqttSignerFactory = mqttSignerFactory;
    }

    /// <inheritdoc/>
    public MqttConnectParams GenerateMqttConnectParams(MqttSignParams signParams, string secret)
    {
        var mqttSigner = MqttSignerFactory.CreateMqttSigner(signParams.AuthType);
        return mqttSigner.GenerateMqttConnectParams(signParams, secret);
    }

    /// <inheritdoc/>
    public MqttAuthResult ParseMqttSignParams(string mqttClientId, string mqttUserName)
    {
        var errorResults = new List<MqttAuthResult>();

        var signParams = MqttSignHelper.ParseMqttClientIdAndUserName(mqttClientId, mqttUserName);
        if (signParams == null)
        {
            return MqttAuthResult.Failed(
                IoTMqttErrorCodes.SignParamsInvalid, "" +
                "Failed to parse MQTT sign parameters from client ID and username.");
        }

        // 校验 MQTT 签名通用参数
        var validateCommonParamsResult = MqttSignHelper.ValidateMqttSignCommonParams(signParams);
        if (!validateCommonParamsResult.Succeeded)
        {
            errorResults.Add(validateCommonParamsResult);
        }

        return errorResults.Count > 0
          ? MqttAuthResult.Combine(errorResults.ToArray())
          : MqttAuthResult.Success(signParams);
    }

    /// <inheritdoc/>
    public MqttAuthResult VerifyMqttSign(MqttConnectParams connectParams, string secret)
    {
        var errorResults = new List<MqttAuthResult>();

        var parsedResult = ParseMqttSignParams(connectParams.ClientId, connectParams.UserName);
        if (parsedResult.Succeeded)
        {
            var signParams = parsedResult.Data!;
            var mqttSigner = MqttSignerFactory.CreateMqttSigner(signParams.AuthType);

            var verifyResult = mqttSigner.VerifyMqttSign(connectParams, secret);
            if (!verifyResult.Succeeded)
            {
                errorResults.Add(verifyResult);
            }
        }
        else
        {
            errorResults.Add(parsedResult);
        }

        return errorResults.Count > 0
             ? MqttAuthResult.Combine(errorResults.ToArray())
             : MqttAuthResult.Success(parsedResult.Data);
    }

}
