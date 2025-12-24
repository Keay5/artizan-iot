using Volo.Abp.Reflection;

namespace Artizan.IoT.Permissions;

public class IoTPermissions
{
    public const string GroupName = "IoT";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IoTPermissions));
    }
}
