using Volo.Abp.Modularity;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTDomainModule),
    typeof(IoTTestBaseModule)
)]
public class IoTDomainTestModule : AbpModule
{

}
