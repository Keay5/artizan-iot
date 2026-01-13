using Volo.Abp.Reflection;

namespace Artizan.IoTHub.Permissions;

public class IoTHubPermissions
{
    public const string GroupName = "IoTHub";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IoTHubPermissions));
    }
}
