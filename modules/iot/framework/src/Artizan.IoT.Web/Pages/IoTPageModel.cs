using Artizan.IoT.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Artizan.IoT.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class IoTPageModel : AbpPageModel
{
    protected IoTPageModel()
    {
        LocalizationResourceType = typeof(IoTResource);
        ObjectMapperContext = typeof(IoTWebModule);
    }
}
