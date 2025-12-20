using Artizan.IoT.ThingModels.Tsls.DataObjects;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace Artizan.IoT.ThingModels.Tsls.Extensions;

public static class DataObjectExtensions
{
    /// <summary>
    /// 将 DataTypeBaseDo 转换为 DataType（基础转换逻辑）
    /// </summary>
    /// <param name="dataTypeBaseDo">待转换的 DataTypeBaseDo 实例</param>
    /// <returns>转换后的 DataType 实例</returns>
    public static DataType ConvertToDataType(this DataTypeBaseDo dataTypeBaseDo)
    {
        Check.NotNull(dataTypeBaseDo, nameof(dataTypeBaseDo));

        // 验证规格数据对象(DataObject)类型与数据类型匹配
        SpecsDoFactory.ValidateSpecsDoType(
            dataTypeBaseDo.DataType,
            dataTypeBaseDo.SpecsDo);

        // 获取对应的数据类型转换器
        var specsConverter = SpecsConverterFactory.GetSpecsConverter(dataTypeBaseDo.DataType);

        // 转换规格数据对象(DataObject)
        var specs = specsConverter.Convert(dataTypeBaseDo.SpecsDo);

        return new DataType
        {
            Type = dataTypeBaseDo.DataType,
            Specs = specs
        };
    }

    public static CommonInputParam ConvertToInputParam(this CommonInputParamDo inputParamDo)
    {
        Check.NotNull(inputParamDo, nameof(inputParamDo));

        return new CommonInputParam
        {
            Identifier = inputParamDo.Identifier,
            Name = inputParamDo.Name,
            Required = inputParamDo.Required,
            DataType = inputParamDo.ConvertToDataType()
        };
    }

    /// <summary>
    /// 将 OutputParamDo 转换为 OutputParam
    /// </summary>
    public static OutputParam ConvertToOutputParam(this OutputParamDo outputParamDo)
    {
        Check.NotNull(outputParamDo, nameof(outputParamDo));

        return new OutputParam
        {
            Identifier = outputParamDo.Identifier,
            Name = outputParamDo.Name,
            DataType = outputParamDo.ConvertToDataType() // 复用基础转换逻辑
        };
    }

    /// <summary>
    /// 将 InputParamDo 集合转换为 InputParam 集合
    /// </summary>
    public static List<CommonInputParam> ConvertToInputParams(this IEnumerable<CommonInputParamDo> inputParamDos)
    {
        Check.NotNull(inputParamDos, nameof(inputParamDos));

        return inputParamDos
            .Select(paramDo => paramDo.ConvertToInputParam()) 
            .ToList(); 
    }

    /// <summary>
    /// 将 OutputParamDo 集合转换为 OutputParam 集合
    /// </summary>
    public static List<OutputParam> ConvertToOutputParams(this IEnumerable<OutputParamDo> outputParamDos)
    {
        Check.NotNull(outputParamDos, nameof(outputParamDos));

        return outputParamDos.Select(paramDo => paramDo.ConvertToOutputParam()).ToList();
    }
}