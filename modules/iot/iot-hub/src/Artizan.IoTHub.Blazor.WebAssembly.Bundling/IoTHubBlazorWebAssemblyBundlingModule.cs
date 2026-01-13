using Volo.Abp.AspNetCore.Components.WebAssembly.Theming.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.Modularity;

namespace Artizan.IoTHub.Blazor.WebAssembly.Bundling;

[DependsOn(
    typeof(AbpAspNetCoreComponentsWebAssemblyThemingBundlingModule)
)]
public class IoTHubBlazorWebAssemblyBundlingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBundlingOptions>(options =>
        {
            var globalStyles = options.StyleBundles.Get(BlazorWebAssemblyStandardBundles.Styles.Global);
            globalStyles.AddContributors(typeof(IoTHubBundleStyleContributor));

            var globalScripts = options.ScriptBundles.Get(BlazorWebAssemblyStandardBundles.Scripts.Global);
            globalScripts.AddContributors(typeof(IoTHubBundleScriptContributor));
        });
    }
}