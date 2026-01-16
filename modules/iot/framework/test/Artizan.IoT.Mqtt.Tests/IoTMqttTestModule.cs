using Artizan.IoT.Mqtt.Auth;
using Artizan.IoT.Mqtt.Auth.Signs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Artizan.IoT.Mqtt.Tests;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpGuidsModule)
)]
public class IoTMqttTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysAllowAuthorization();

        // 注册所有MQTT签名器
        context.Services.AddSingleton<MqttSignerFactory>();
        context.Services.AddTransient<OneDeviceOneSecretMqttSigner>();
        context.Services.AddTransient<OneProductOneSecretMqttSigner>();
        // 注册MQTT认证管理器
        context.Services.AddSingleton<IMqttSignAuthManager, MqttSignAuthManager>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        SeedTestData(context);
    }

    private static void SeedTestData(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
        {
            using (var scope = context.ServiceProvider.CreateScope())
            {
                //await scope.ServiceProvider
                //    .GetRequiredService<IDataSeeder>()
                //    .SeedAsync();
            }
        });
    }
}
