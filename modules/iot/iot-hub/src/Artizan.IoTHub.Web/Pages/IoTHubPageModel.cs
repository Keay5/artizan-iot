using Artizan.IoTHub.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Artizan.IoTHub.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class IoTHubPageModel : AbpPageModel
{
    protected IoTHubPageModel()
    {
        LocalizationResourceType = typeof(IoTHubResource);
        ObjectMapperContext = typeof(IoTHubWebModule);
    }
}
