using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class IoTDAInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTDAInstallerModule>();
        });
    }
}
