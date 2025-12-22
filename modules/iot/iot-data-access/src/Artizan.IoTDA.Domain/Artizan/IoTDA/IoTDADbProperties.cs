namespace Artizan.IoTDA;

public static class IoTDADbProperties
{
    public static string DbTablePrefix { get; set; } = "IoTDA";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "IoTDA";
}
