namespace Artizan.IoT;

public static class IoTDbProperties
{
    public static string DbTablePrefix { get; set; } = "IoT";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "IoT";
}
