using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class IoTApplicationContractsModule : AbpModule
{

}
