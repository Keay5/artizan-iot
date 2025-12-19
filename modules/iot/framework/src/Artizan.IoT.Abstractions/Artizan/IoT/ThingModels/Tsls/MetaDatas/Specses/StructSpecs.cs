using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

/// <summary>
/// 结构体规格
/// </summary>
public class StructSpecs : List<StructField>, ISpecs
{
    public StructSpecs()
    {
    }

    public StructSpecs(IEnumerable<StructField> collection) : base(collection)
    {
    }
}

/// <summary>
/// 结构体字段
/// </summary>
public class StructField
{
    [JsonProperty("identifier", Order = 1)]
    public string Identifier { get; set; }

    [JsonProperty("name", Order = 2)]
    public string Name { get; set; }

    [JsonProperty("dataType", Order = 3)]
    public DataType DataType { get; set; }
}
