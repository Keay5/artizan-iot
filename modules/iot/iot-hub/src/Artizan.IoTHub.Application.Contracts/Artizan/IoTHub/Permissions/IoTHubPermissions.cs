using Volo.Abp.Reflection;

namespace Artizan.IoTHub.Permissions;

public class IoTHubPermissions
{
    public const string GroupName = "IoTHub";
    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string Management = Default + ".Management";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
        public const string Update = Default + ".Update";


        public static class Modules
        {
            public const string Default = Products.Default + ".Functions";
            public const string Management = Default + ".Management";
            public const string Create = Default + ".Create";
            public const string Delete = Default + ".Delete";
            public const string Update = Default + ".Update";

            public static class Properties 
            {
                public const string Default = Modules.Default + ".Properties";
                public const string Create = Default + ".Create";
                public const string Delete = Default + ".Delete";
                public const string Update = Default + ".Update";
            }

            public static class Services
            {
                public const string Default = Modules.Default + ".Services";
                public const string Create = Default + ".Create";
                public const string Delete = Default + ".Delete";
                public const string Update = Default + ".Update";
            }

            public static class Events
            {
                public const string Default = Modules.Default + ".Events";
                public const string Create = Default + ".Create";
                public const string Delete = Default + ".Delete";
                public const string Update = Default + ".Update";
            }
        }
    }

    public static class Devices
    {
        public const string Default = GroupName + ".Devices";
        public const string Management = Default + ".Management";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
        public const string Update = Default + ".Update";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IoTHubPermissions));
    }
}
