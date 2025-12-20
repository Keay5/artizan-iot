using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using System;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// 标记数据类型(<see cref="DataTypes"/>) 与类型(<see cref="ISpecsDo"/>)的关联特性
/// 
/// <seealso cref="SpecsConverterFactory"/> 
/// </summary>
// 
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SpecsDoMapsToDataTypesAttribute : Attribute
{
    public DataTypes[] DataTypes { get; }
    public SpecsDoMapsToDataTypesAttribute(params DataTypes[] dataTypes)
    {
        DataTypes = dataTypes;
    }
}
