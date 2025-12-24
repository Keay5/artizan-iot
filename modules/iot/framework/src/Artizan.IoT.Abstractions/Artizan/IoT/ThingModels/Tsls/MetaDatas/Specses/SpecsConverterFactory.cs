using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;

/// <summary>
/// SpecsConverter 策略工厂，通过<see cref="Enums.DataTypes"/> 自动匹配对应的转换器<see cref="ISpecsConverter"/>
/// </summary>
public static class SpecsConverterFactory
{
    private static readonly Lazy<IReadOnlyDictionary<Enums.DataTypes, ISpecsConverter>> _lazyDataTypeToSpecsConverterMaps = 
            new Lazy<IReadOnlyDictionary<Enums.DataTypes, ISpecsConverter>>(BuildDataTypeToSpecsConverterMaps);

    public static IReadOnlyDictionary<Enums.DataTypes, ISpecsConverter> DataTypeToSpecsConverterMaps => _lazyDataTypeToSpecsConverterMaps.Value;

    public static ISpecsConverter GetSpecsConverter(Enums.DataTypes dataType)
    {
        if (DataTypeToSpecsConverterMaps.TryGetValue(dataType, out var specsConverter))
        {
            return specsConverter;
        }

        throw new KeyNotFoundException($"未找到Enums.DataTypes.{dataType}对应的转换器");
    }

    private static IReadOnlyDictionary<Enums.DataTypes, ISpecsConverter> BuildDataTypeToSpecsConverterMaps()
    {
        var specsConverters = new Dictionary<Enums.DataTypes, ISpecsConverter>();
        var assembly = typeof(ISpecsConverter).Assembly;

        // 扫描所有 ISpecsConverter<TSpecsDo, TSpecs>实现类
        var specsConverterTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Select(t => new
            {
                Type = t,
                Interfaces = t.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISpecsConverter<,>))
            })
            .Where(x => x.Interfaces.Any());

        foreach (var specsConverterTypeInfo in specsConverterTypes)
        {
            // specsConverterInterfaceType指的是ISpecsConverter<TSpecsDo, TSpecs>
            foreach (var specsConverterInterfaceType in specsConverterTypeInfo.Interfaces)
            {
                // 获取接口 ISpecsConverter<TSpecsDo, TSpecs> 的第一个参数类型，即：TSpecsDo
                var specsDoType = specsConverterInterfaceType.GetGenericArguments()[0];

                // 获取 TSpecsDo 类型标记的特性
                var specsDoAttribute = specsDoType.GetCustomAttribute<SpecsDoMapsToDataTypesAttribute>();
                if (specsDoAttribute == null)
                {
                    continue;
                }

                // 创建 SpecsConverter 转换器实例
                if (Activator.CreateInstance(specsConverterTypeInfo.Type) is not ISpecsConverter specsConverter)
                {
                    continue;
                }

                // 从特性中获取 SpecsDo 对应E 的nums.DataTypes，注册到对应 
                foreach (var dataType in specsDoAttribute.DataTypes)
                {
                    specsConverters[dataType] = specsConverter;
                }
            }
        }

        return new ReadOnlyDictionary<Enums.DataTypes, ISpecsConverter>(specsConverters);
    }

}
