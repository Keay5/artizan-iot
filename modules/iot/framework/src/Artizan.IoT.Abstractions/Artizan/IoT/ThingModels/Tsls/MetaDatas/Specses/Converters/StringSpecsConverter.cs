using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

/// <summary>
/// StringSpecs 转换器（处理文本类型参数）
/// </summary>
public class StringSpecsConverter : ISpecsConverter<StringSpecsDo, StringSpecs>
{
    public StringSpecs Convert(StringSpecsDo param)
    {
        return new StringSpecs(param.Length);
    }

    public ISpecs Convert(ISpecsDo param)
    {
        return Convert((StringSpecsDo)param);
    }
}
