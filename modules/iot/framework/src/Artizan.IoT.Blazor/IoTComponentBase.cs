using Artizan.IoT.Localization;
using Volo.Abp.AspNetCore.Components;

namespace Artizan.IoT.Blazor;

public abstract class IoTComponentBase : AbpComponentBase
{
    protected IoTComponentBase()
    {
        LocalizationResource = typeof(IoTResource);
    }
}