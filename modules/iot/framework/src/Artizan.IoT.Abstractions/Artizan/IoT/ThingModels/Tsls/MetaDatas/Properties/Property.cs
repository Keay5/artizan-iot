using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;

public class Property
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public AccessModes AccessMode { get; set; }
    public bool Required { get; set; }
    public DataType DataType { get; set; }
    public string? Desc { get; set; }

    public bool IsValidAccessMode()
    {
        return Enum.IsDefined(typeof(AccessModes), AccessMode);
    }
}
