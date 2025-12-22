using System.Text.Json.Serialization;

namespace Artizan.IoT.Alinks.DataObjectsOld;

public class AlinkRequestBase
{
    /// <summary>
    /// 消息ID号。String类型的数字，取值范围0~4294967295，且每个消息ID在当前设备中具有唯一性。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}
 