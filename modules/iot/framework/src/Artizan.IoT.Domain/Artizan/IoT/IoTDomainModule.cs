using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Artizan.IoT;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(IoTDomainSharedModule)
)]
public class IoTDomainModule : AbpModule
{

}
