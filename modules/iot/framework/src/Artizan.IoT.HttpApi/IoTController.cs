using Artizan.IoT.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Artizan.IoT;

public abstract class IoTController : AbpControllerBase
{
    protected IoTController()
    {
        LocalizationResource = typeof(IoTResource);
    }
}
