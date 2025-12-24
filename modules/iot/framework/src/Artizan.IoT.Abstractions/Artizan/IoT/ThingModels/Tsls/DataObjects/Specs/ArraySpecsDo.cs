using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// Array 类型的参数（需要 size 和元素类型）
/// 数组型规格的构建数据对象（用于转换为ArraySpecs）
/// </summary>
[SpecsDoMapsToDataTypes(DataTypes.Array)]
public class ArraySpecsDo : ISpecsDo
{
    public string Size { get; set; }
    public DataTypes ItemType { get; set; } // 数组元素的数据类型
    public ISpecsDo ItemSpecs { get; set; } // 元素的 Specs 参数（如元素是int，这里传 NumericSpecsDo）
}
