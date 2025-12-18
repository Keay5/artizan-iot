using Artizan.IoTHub.Localization;
using Volo.Abp.Application.Services;

namespace Artizan.IoTHub;

public abstract class IoTHubAppServiceBase : ApplicationService
{
    protected IoTHubAppServiceBase()
    {
        LocalizationResource = typeof(IoTHubResource);
        ObjectMapperContext = typeof(IoTHubApplicationModule);
    }
}
