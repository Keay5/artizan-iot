using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Text.Json.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects;

public class DataTypeBaseDo : IHasDataType
{
    public DataTypes DataType { get; set; }

    // 用string接收JSON格式的SpecsDo
    public string SpecsDoJsonString { get; set; }
    [JsonIgnore]
    public ISpecsDo SpecsDo => SpecsDoFactory.DeserializeSpecsDo(DataType, SpecsDoJsonString);
}
