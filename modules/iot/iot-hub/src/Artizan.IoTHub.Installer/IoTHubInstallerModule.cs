using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class IoTHubInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTHubInstallerModule>();
        });
    }
}
