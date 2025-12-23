using Artizan.IoTDA.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Artizan.IoTDA;

public abstract class IoTDAController : AbpControllerBase
{
    protected IoTDAController()
    {
        LocalizationResource = typeof(IoTDAResource);
    }
}
