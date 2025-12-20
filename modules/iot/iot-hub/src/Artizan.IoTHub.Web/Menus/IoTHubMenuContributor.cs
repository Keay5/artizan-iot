using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Artizan.IoTHub.Web.Menus;

public class IoTHubMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(new ApplicationMenuItem(IoTHubMenus.Prefix, displayName: "IoTHub", "~/IoTHub", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
