using System;

namespace Artizan.IoT.Products.MessageParsings.Etos;

[Serializable]
public class CustomTopicRawDataParsedEto
{
    public string TrackId { get; }
    public string Topic { get; }
    public string JsonData { get; }
    public string ProductKey { get; set; }
    public string DeviceName { get; set; }

    protected CustomTopicRawDataParsedEto()
    {
    }

    public CustomTopicRawDataParsedEto(
        string trackId,
        string topic,
        string jsonData,
        string productKey,
        string deviceName)
    {
        TrackId = trackId;
        Topic = topic;
        JsonData = jsonData;
        ProductKey = productKey;
        DeviceName = deviceName;
    }
}
