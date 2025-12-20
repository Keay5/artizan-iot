using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

// 转换器泛型接口（强关联数据对象和规格类型）
public interface ISpecsConverter<in TSpecsDo, out TSpecs> : ISpecsConverter
    where TSpecsDo : ISpecsDo
    where TSpecs : ISpecs
{
    TSpecs Convert(TSpecsDo specsDo);
}

// 转换器基础接口
public interface ISpecsConverter
{
    ISpecs Convert(ISpecsDo specsDo);
}
