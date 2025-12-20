using Volo.Abp.Modularity;

namespace Artizan.IoTDA;

[DependsOn(
    typeof(IoTDAApplicationModule),
    typeof(IoTDADomainTestModule)
    )]
public class IoTDAApplicationTestModule : AbpModule
{

}
