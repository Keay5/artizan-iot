using Artizan.IoT.Localization;
using Volo.Abp.Application.Services;

namespace Artizan.IoT;

public abstract class IoTAppService : ApplicationService
{
    protected IoTAppService()
    {
        LocalizationResource = typeof(IoTResource);
        ObjectMapperContext = typeof(IoTApplicationModule);
    }
}
