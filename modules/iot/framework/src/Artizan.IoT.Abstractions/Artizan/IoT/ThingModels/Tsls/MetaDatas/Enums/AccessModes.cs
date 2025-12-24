using System.Runtime.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

public enum AccessModes
{
    [EnumMember(Value = "r")]
    ReadOnly,

    [EnumMember(Value = "rw")]
    ReadAndWrite
}
