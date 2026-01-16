using Artizan.IoTHub.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Artizan.IoTHub.Permissions;

public class IoTHubPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(IoTHubPermissions.GroupName, L("Permission:IoTHub"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<IoTHubResource>(name);
    }
}
