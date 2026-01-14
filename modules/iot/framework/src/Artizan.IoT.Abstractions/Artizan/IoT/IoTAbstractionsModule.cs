using Volo.Abp.Modularity;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTCoreModule)
)]
public class IoTAbstractionsModule : AbpModule
{

}