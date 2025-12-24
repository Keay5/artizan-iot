using Artizan.IoT;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(IoTDADomainSharedModule),
    typeof(IoTAbstractionsModule)
)]
public class IoTDADomainModule : AbpModule
{

}
