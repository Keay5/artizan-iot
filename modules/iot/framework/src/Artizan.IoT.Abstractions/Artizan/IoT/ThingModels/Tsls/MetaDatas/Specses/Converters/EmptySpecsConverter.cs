using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

/// <summary>
/// EmptySpecs 转换器（处理日期类型等无参数场景）
/// </summary>
public class EmptySpecsConverter : ISpecsConverter<EmptySpecsDo, EmptySpecs>
{
    public EmptySpecs Convert(EmptySpecsDo param)
    {
        // 空规格无需参数，直接返回空实例
        return new EmptySpecs();
    }

    public ISpecs Convert(ISpecsDo param)
    {
        return Convert((EmptySpecsDo)param);
    }
}
