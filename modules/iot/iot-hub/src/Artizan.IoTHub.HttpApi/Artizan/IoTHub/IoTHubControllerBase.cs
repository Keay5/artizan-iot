using Artizan.IoTHub.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Artizan.IoTHub;

public abstract class IoTHubControllerBase : AbpControllerBase
{
    protected IoTHubControllerBase()
    {
        LocalizationResource = typeof(IoTHubResource);
    }
}
