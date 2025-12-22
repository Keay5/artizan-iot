using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
///  Int32/float/double 类型的参数（需要 min/max/step/unit 等）
/// 数值型规格的构建参数（用于转换为NumericSpecs）
/// 承载未校验的原始输入数据
/// </summary>
[SpecsDoMapsToDataTypes(DataTypes.Int32, DataTypes.Float, DataTypes.Double)]
public class NumericSpecsDo : ISpecsDo
{
    public string Min { get; set; }
    public string Max { get; set; }
    public string? Step { get; set; }
    public string? Unit { get; set; }
    public string? UnitName { get; set; }
}

