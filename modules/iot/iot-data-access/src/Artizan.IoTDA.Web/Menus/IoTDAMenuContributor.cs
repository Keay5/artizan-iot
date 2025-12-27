using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Artizan.IoTDA.Web.Menus;

public class IoTDAMenuContributor : IMenuContributor
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
        context.Menu.AddItem(new ApplicationMenuItem(IoTDAMenus.Prefix, displayName: "IoTDA", "~/IoTDA", icon: "fa fa-globe"));

        return Task.CompletedTask;
    }
}
