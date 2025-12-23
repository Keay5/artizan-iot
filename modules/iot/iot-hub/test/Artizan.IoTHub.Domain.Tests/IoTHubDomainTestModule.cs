using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubDomainModule),
    typeof(IoTHubTestBaseModule)
)]
public class IoTHubDomainTestModule : AbpModule
{

}
