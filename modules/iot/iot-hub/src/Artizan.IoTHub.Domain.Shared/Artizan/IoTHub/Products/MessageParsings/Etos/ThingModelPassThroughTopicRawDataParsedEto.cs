using System;

namespace Artizan.IoTHub.Products.MessageParsings.Etos;

[Serializable]
public class ThingModelPassThroughTopicRawDataParsedEto
{
    public string TrackId { get; }
    public string MessageId { get; }
    public string Topic { get; }
    public string AlinkJsonData { get; }
    public string ProductKey { get; set; }
    public string DeviceName { get; set; }

    protected ThingModelPassThroughTopicRawDataParsedEto()
    {
    }

    public ThingModelPassThroughTopicRawDataParsedEto(
        string trackId,
        string topic,
        string alinkJsonData,
        string productKey,
        string deviceName)
    {
        TrackId = trackId;
        Topic = topic;
        AlinkJsonData = alinkJsonData;
        ProductKey = productKey;
        DeviceName = deviceName;
    }
}
