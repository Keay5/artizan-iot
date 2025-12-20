using Artizan.IoTDA.Localization;
using Volo.Abp.Application.Services;

namespace Artizan.IoTDA;

public abstract class IoTDAAppService : ApplicationService
{
    protected IoTDAAppService()
    {
        LocalizationResource = typeof(IoTDAResource);
        ObjectMapperContext = typeof(IoTDAApplicationModule);
    }
}
