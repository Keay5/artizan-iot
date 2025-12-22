using Volo.Abp.Modularity;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTApplicationModule),
    typeof(IoTDomainTestModule)
    )]
public class IoTApplicationTestModule : AbpModule
{

}
