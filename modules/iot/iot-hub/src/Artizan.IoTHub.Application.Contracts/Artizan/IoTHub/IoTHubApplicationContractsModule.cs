using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Artizan.IoTHub;

[DependsOn(
    typeof(IoTHubDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class IoTHubApplicationContractsModule : AbpModule
{

}
