using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class IoTInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTInstallerModule>();
        });
    }
}
