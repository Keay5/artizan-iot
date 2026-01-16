using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Auth.Signs;

/// <summary>
/// MQTT签名器工厂
/// </summary>
public class MqttSignerFactory : ISingletonDependency
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;

    public MqttSignerFactory(IAbpLazyServiceProvider lazyServiceProvider)
    {
        LazyServiceProvider = lazyServiceProvider;
    }

    public IMqttSigner CreateMqttSigner(MqttAuthType authType)
    {
        return authType switch
        {
            MqttAuthType.OneDeviceOneSecret =>
                LazyServiceProvider.LazyGetRequiredService<OneDeviceOneSecretMqttSigner>(),

            MqttAuthType.OneProductOneSecretPreRegister or MqttAuthType.OneProductOneSecretNoPreRegister =>
                CreateOneProductOneSecretSigner(authType),

            _ => throw new AbpException($"不支持的MQTT认证类型：{authType}")
        };
    }

    private OneProductOneSecretMqttSigner CreateOneProductOneSecretSigner(MqttAuthType authType)
    {
        return ActivatorUtilities.CreateInstance<OneProductOneSecretMqttSigner>(
            LazyServiceProvider,
            authType
        );
    }
}
