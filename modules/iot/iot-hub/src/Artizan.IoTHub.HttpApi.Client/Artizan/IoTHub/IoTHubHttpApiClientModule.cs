using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class IoTHubHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(IoTHubApplicationContractsModule).Assembly,
            IoTHubRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTHubHttpApiClientModule>();
        });

    }
}
