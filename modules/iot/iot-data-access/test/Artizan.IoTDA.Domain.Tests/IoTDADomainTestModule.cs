using Volo.Abp.Modularity;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDADomainModule),
    typeof(IoTDATestBaseModule)
)]
public class IoTDADomainTestModule : AbpModule
{

}
