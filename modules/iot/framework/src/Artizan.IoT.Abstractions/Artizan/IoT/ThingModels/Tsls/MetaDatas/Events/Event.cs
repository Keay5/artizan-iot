using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Collections.Generic;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
public class Event
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string? Desc { get; set; }
    /// <summary>
    /// 事件类型（info/alert/error）
    /// </summary>
    public EventTypes Type { get; set; }
    public bool Required { get; set; }
    public string Method { get; set; }
    /// <summary>
    /// 事件输出参数
    /// </summary>
    public List<OutputParam>? OutputData { get; set; }
}
