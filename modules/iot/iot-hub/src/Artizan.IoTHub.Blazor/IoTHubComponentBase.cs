using Artizan.IoTHub.Localization;
using Volo.Abp.AspNetCore.Components;

namespace Artizan.IoTHub.Blazor;

public abstract class IoTHubComponentBase : AbpComponentBase
{
    protected IoTHubComponentBase()
    {
        LocalizationResource = typeof(IoTHubResource);
    }
}