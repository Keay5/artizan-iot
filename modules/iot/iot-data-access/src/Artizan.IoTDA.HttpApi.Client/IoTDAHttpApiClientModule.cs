using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDAApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class IoTDAHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(IoTDAApplicationContractsModule).Assembly,
            IoTDARemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTDAHttpApiClientModule>();
        });

    }
}
