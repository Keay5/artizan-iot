using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubApplicationModule),
    typeof(IoTHubDomainTestModule)
    )]
public class IoTHubApplicationTestModule : AbpModule
{

}
