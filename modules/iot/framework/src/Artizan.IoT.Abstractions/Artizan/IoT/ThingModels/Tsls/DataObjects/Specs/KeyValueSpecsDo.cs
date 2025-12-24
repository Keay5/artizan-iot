using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Collections.Generic;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// Bool/enum 类型的参数（需要键值对，如 "0":"关"）
/// </summary>
[SpecsDoMapsToDataTypes(DataTypes.Boolean, DataTypes.Enum)]
public class KeyValueSpecsDo : ISpecsDo
{
    public Dictionary<string, string> Values { get; set; } = new();
}
