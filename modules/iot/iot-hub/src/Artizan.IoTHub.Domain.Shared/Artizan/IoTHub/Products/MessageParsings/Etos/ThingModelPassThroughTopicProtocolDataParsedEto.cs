using System;

namespace Artizan.IoTHub.Products.MessageParsings.Etos;

[Serializable]
public class ThingModelPassThroughTopicProtocolDataParsedEto
{
    public string TrackId { get; }
    public string Topic { get; }
    public byte[] RawData { get; }
    public string ProductKey { get; set; }
    public string DeviceName { get; set; }

    protected ThingModelPassThroughTopicProtocolDataParsedEto()
    {
    }

    public ThingModelPassThroughTopicProtocolDataParsedEto(
        string trackId,
        string topic,
        byte[] rawData,
        string productKey,
        string deviceName)
    {
        TrackId = trackId;
        Topic = topic;
        RawData = rawData;
        ProductKey = productKey;
        DeviceName = deviceName;
    }
}
