using Artizan.IoTDA.Localization;
using Volo.Abp.AspNetCore.Components;

namespace Artizan.IoTDA.Blazor;

public abstract class IoTDAComponentBase : AbpComponentBase
{
    protected IoTDAComponentBase()
    {
        LocalizationResource = typeof(IoTDAResource);
    }
}