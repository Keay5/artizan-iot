using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Text.Json.Serialization;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects;

public interface IHasDataType
{
    DataTypes DataType { get; set; }

    // 用string接收JSON格式的SpecsDo
    string SpecsDoJsonString { get; set; }
    [JsonIgnore]
    ISpecsDo SpecsDo { get; }
}
