namespace Artizan.IoT.Thing;

public interface IThingIdentifierGenerator
{
    string Generate(string productKey, string deviceName);
    (string ProductKey, string DeviceName) Parse(string thingIdentifier);
}
