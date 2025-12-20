using Artizan.IoT.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Artizan.IoT.Permissions;

public class IoTPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(IoTPermissions.GroupName, L("Permission:IoT"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IoTResource>(name);
    }
}
