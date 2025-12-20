using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDADomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class IoTDAApplicationContractsModule : AbpModule
{

}
