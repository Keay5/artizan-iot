using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// Text 类型的参数（需要长度）
/// </summary>
[SpecsDoMapsToDataTypes(DataTypes.Text)]
public class StringSpecsDo : ISpecsDo
{
    public string Length { get; set; }
}
