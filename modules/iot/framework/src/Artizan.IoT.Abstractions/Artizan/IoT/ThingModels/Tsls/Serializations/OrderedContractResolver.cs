using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Artizan.IoT.ThingModels.Tsls.Serializations;

/// <summary>
/// 保持属性定义顺序的ContractResolver
/// </summary>
public class OrderedContractResolver : CamelCasePropertyNamesContractResolver//DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(System.Type type, MemberSerialization memberSerialization)
    {
        return base.CreateProperties(type, memberSerialization)
            .OrderBy(p => p.Order ?? int.MaxValue)
            .ToList();
    }
}
