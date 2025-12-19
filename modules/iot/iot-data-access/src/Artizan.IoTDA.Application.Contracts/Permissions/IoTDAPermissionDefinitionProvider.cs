using Artizan.IoTDA.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Artizan.IoTDA.Permissions;

public class IoTDAPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(IoTDAPermissions.GroupName, L("Permission:IoTDA"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IoTDAResource>(name);
    }
}
