using Artizan.IoTDA.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Artizan.IoTDA.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class IoTDAPageModel : AbpPageModel
{
    protected IoTDAPageModel()
    {
        LocalizationResourceType = typeof(IoTDAResource);
        ObjectMapperContext = typeof(IoTDAWebModule);
    }
}
