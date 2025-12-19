using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;


namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

// 数组类型的转换策略（需递归处理元素类型）
public class ArraySpecsConverter : ISpecsConverter<ArraySpecsDo, ArraySpecs>
{
    public ArraySpecs Convert(ArraySpecsDo specsDo)
    {
        // 通过工厂获取转换器（此时字典已完全初始化）
        var itemConverter = SpecsConverterFactory.GetSpecsConverter(specsDo.ItemType);
        var itemSpecs = specsDo.ItemSpecs != null ? itemConverter.Convert(specsDo.ItemSpecs) : null;

        return new ArraySpecs(size: specsDo.Size, new DataType
        {
            Type = specsDo.ItemType,
            Specs = itemSpecs
        });
    }

    public ISpecs Convert(ISpecsDo param)
    {
        return Convert((ArraySpecsDo)param);
    }
}
