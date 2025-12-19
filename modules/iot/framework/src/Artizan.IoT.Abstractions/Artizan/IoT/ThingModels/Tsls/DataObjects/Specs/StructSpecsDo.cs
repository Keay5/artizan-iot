using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Collections.Generic;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// Struct 类型的参数（需要结构体字段）
/// </summary>
[SpecsDoMapsToDataTypes(DataTypes.Struct)]
public class StructSpecsDo : List<StructFieldDo>, ISpecsDo
{

}

/// <summary>
/// 结构体字段的参数
/// </summary>
public class StructFieldDo
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public DataTypes DataType { get; set; }
    public ISpecsDo? SpecsDo { get; set; }
}