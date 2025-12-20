using Volo.Abp.Reflection;

namespace Artizan.IoTDA.Permissions;

public class IoTDAPermissions
{
    public const string GroupName = "IoTDA";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IoTDAPermissions));
    }
}
