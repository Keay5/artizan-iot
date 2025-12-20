using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using Newtonsoft.Json;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;

[JsonConverter(typeof(DataTypeConverter))]
public class DataType
{
    public Enums.DataTypes Type { get; set; }
    public ISpecs Specs { get; set; }
}