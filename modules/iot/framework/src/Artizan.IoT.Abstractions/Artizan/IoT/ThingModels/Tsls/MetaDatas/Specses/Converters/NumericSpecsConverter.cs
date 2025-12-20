using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

/// <summary>
/// 数值类型（int/float/double）的转换策略
/// </summary>
public class NumericSpecsConverter : ISpecsConverter<NumericSpecsDo, NumericSpecs>
{
    public NumericSpecs Convert(NumericSpecsDo specsDo)
    {
        return new NumericSpecs(
          min: specsDo.Min,
          max: specsDo.Max,
          step: specsDo.Step,
          unit: specsDo.Unit,
          unitName: specsDo.UnitName
      );
    }

    public ISpecs Convert(ISpecsDo param)
    {
        return Convert((NumericSpecsDo)param);
    }

}
