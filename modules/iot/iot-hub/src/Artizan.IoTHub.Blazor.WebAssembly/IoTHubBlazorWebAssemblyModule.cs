using Volo.Abp.AspNetCore.Components.WebAssembly.Theming;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub.Blazor.WebAssembly;

[DependsOn(
    typeof(IoTHubBlazorModule),
    typeof(IoTHubHttpApiClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyThemingModule)
    )]
public class IoTHubBlazorWebAssemblyModule : AbpModule
{

}
