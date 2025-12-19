using Volo.Abp.AspNetCore.Components.WebAssembly.Theming;
using Volo.Abp.Modularity;

namespace Artizan.IoT.Blazor.WebAssembly;

[DependsOn(
    typeof(IoTBlazorModule),
    typeof(IoTHttpApiClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyThemingModule)
    )]
public class IoTBlazorWebAssemblyModule : AbpModule
{

}
