using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using System.Linq;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

/// <summary>
/// StructSpecs 转换器（处理结构体类型参数）
/// </summary>
public class StructSpecsConverter : ISpecsConverter<StructSpecsDo, StructSpecs>
{
    public StructSpecs Convert(StructSpecsDo param)
    {
        return new StructSpecs(
            param.Select(f => new StructField
            {
                Identifier = f.Identifier,
                Name = f.Name,
                DataType = new DataType
                {
                    Type = f.DataType,
                    // 通过工厂获取字段类型的转换器（确保字典已完全初始化）
                    Specs = f.SpecsDo != null
                        ? SpecsConverterFactory.GetSpecsConverter(f.DataType).Convert(f.SpecsDo)
                        : null
                }
            }).ToList()
        );
    }

    ISpecs ISpecsConverter.Convert(ISpecsDo param)
    {
        return Convert((StructSpecsDo)param);
    }

}
