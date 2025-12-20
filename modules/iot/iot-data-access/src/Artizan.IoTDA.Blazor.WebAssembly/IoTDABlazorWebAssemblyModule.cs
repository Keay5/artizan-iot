using Volo.Abp.AspNetCore.Components.WebAssembly.Theming;
using Volo.Abp.Modularity;

namespace Artizan.IoTDA.Blazor.WebAssembly;

[DependsOn(
    typeof(IoTDABlazorModule),
    typeof(IoTDAHttpApiClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyThemingModule)
    )]
public class IoTDABlazorWebAssemblyModule : AbpModule
{

}
