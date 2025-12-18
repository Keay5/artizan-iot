using System.Collections.Generic;

namespace Artizan.IoTHub.Alinks;

public class PropertyReportPayload
{
    public long Id { get; set; } // 消息ID
    public Dictionary<string, object> Params { get; set; } = new(); // 属性键值对
    public string Method { get; set; } = "thing.event.property.post"; // 方法名
}
