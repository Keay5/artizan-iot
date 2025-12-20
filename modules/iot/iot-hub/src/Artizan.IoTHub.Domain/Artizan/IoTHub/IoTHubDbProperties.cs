namespace Artizan.IoTHub;

public static class IoTHubDbProperties
{
    public static string DbTablePrefix { get; set; } = "IoTHub";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "IoTHub";
}
