using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;

namespace Artizan.IoT.Abstractions.Tests;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpGuidsModule),
    typeof(IoTTestBaseModule),
    typeof(IoTAbstractionsModule)
)]
public class ArtizanIoTAbstractionsTestsModule : AbpModule
{

}