using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Artizan.IoT.ScriptDataCodec.JavaScript;

[DependsOn(
    typeof(IoTScriptDataCodecModule)
)]
public class IoTScriptDataCodecJavaScriptModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddJaveScriptDataCodec();
    }
}