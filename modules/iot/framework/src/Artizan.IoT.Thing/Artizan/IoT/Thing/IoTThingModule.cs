using Volo.Abp.Modularity;

namespace Artizan.IoT.Thing;

[DependsOn(
   typeof(IoTAbstractionsModule)
)]
public class IoTThingModule : AbpModule
{
}