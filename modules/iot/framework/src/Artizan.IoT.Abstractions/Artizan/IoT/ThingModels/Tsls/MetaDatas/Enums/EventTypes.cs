using System.Runtime.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

public enum EventTypes
{
    /// <summary>
    /// 信息
    /// </summary>
    [EnumMember(Value = "info")]
    Info,
    /// <summary>
    /// 告警
    /// </summary>
    [EnumMember(Value = "alert")]
    Alert,
    /// <summary>
    /// 故障
    /// </summary>
    [EnumMember(Value = "error")]
    Error
}
