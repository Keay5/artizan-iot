using Artizan.IoT.ThingModels.Tsls.Builders.Exceptions;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Volo.Abp;

namespace Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

/// <summary>
/// 规格数据对象(DataObject)<see cref="ISpecsDo"/>工厂，
/// 负责数据类型<see cref="DataTypes"/>与规格数据对象(DataObject)类型<see cref="ISpecsDo"/>的映射管理及反序列化
/// 支持运行时动态注册新类型，线程安全设计
/// </summary>
public static class SpecsDoFactory
{
    /// <summary>
    /// 数据类型与规格数据对象(DataObject)类型的映射表（线程安全）,
    /// ConcurrentDictionary，确保多线程环境下动态注册 / 移除类型时的安全性
    /// </summary>
    private static readonly ConcurrentDictionary<DataTypes, Type> _dataTypeToSpecsDoMaps = new(
        new Dictionary<DataTypes, Type>
        {
            { DataTypes.Int32, typeof(NumericSpecsDo) },
            { DataTypes.Float, typeof(NumericSpecsDo) },
            { DataTypes.Double, typeof(NumericSpecsDo) },
            { DataTypes.Boolean, typeof(KeyValueSpecsDo) },
            { DataTypes.Enum, typeof(KeyValueSpecsDo) },
            { DataTypes.Text, typeof(StringSpecsDo) },
            { DataTypes.Date, typeof(EmptySpecsDo) },
            { DataTypes.Array, typeof(ArraySpecsDo) },
            { DataTypes.Struct, typeof(StructSpecsDo) }
        });

    /// <summary>
    /// 注册数据类型<see cref="DataTypes"/>与规格数据对象(DataObject)类型<see cref="ISpecsDo"/>的映射关系.
    /// 支持运行时动态扩展新的数据类型映射，无需修改源码。
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <param name="specsDoType">规格数据对象(DataObject)类型（必须实现<see cref="ISpecsDo"/>）</param>
    /// <exception cref="ArgumentNullException">当specsDoType为null时抛出</exception>
    /// <exception cref="ArgumentException">当specsDoType未实现ISpecsDo时抛出</exception>
    public static void RegisterSpecsDoType(DataTypes dataType, Type specsDoType)
    {
        Check.NotNull(specsDoType, nameof(specsDoType));

        if (!typeof(ISpecsDo).IsAssignableFrom(specsDoType))
            throw new ArgumentException(
                $"类型 {specsDoType.FullName} 必须实现 {nameof(ISpecsDo)} 接口",
                nameof(specsDoType));

        _dataTypeToSpecsDoMaps[dataType] = specsDoType;
    }

    /// <summary>
    /// 移除数据类型<see cref="DataTypes"/>与规格数据对象(DataObject)类型<see cref="ISpecsDo"/>的映射关系
    /// 支持运行时动态扩展新的数据类型映射，无需修改源码。
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <returns>是否移除成功</returns>
    public static bool UnregisterSpecsDoType(DataTypes dataType)
    {
        return _dataTypeToSpecsDoMaps.TryRemove(dataType, out _);
    }

    /// <summary>
    /// 获取指定数据类型<see cref="DataTypes"/>与规格数据对象(DataObject)类型<see cref="ISpecsDo"/>的映射关系
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <returns>规格数据对象(DataObject)类型</returns>
    /// <exception cref="NotSupportedException">当数据类型未注册时抛出</exception>
    public static Type GetSpecDoType(DataTypes dataType)
    {
        if (_dataTypeToSpecsDoMaps.TryGetValue(dataType, out var type))
        {
            return type;
        }

        throw new NotSupportedException($"不支持的数据类型: {dataType}，请先注册映射关系");
    }

    /// <summary>
    /// 根据数据类型<see cref="DataTypes"/>反序列化规格数据对象(DataObject)<see cref="ISpecsDo"/>的JSON字符串
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <param name="specsDoJsonString">规格数据对象(DataObject)JSON字符串</param>
    /// <returns>反序列化后的ISpecsDo实例</returns>
    /// <exception cref="NotSupportedException">当数据类型未注册时抛出</exception>
    /// <exception cref="JsonSerializationException">JSON反序列化失败时抛出</exception>
    public static ISpecsDo DeserializeSpecsDo(DataTypes dataType, string specsDoJsonString)
    {
        // 空JSON处理（仅允许Date类型，其他类型需显式参数）
        if (string.IsNullOrEmpty(specsDoJsonString))
        {
            if (dataType == DataTypes.Date)
            {
                return new EmptySpecsDo();
            }

            throw new ArgumentException(
                $"数据类型 [{dataType}] 不允许空规格数据对象(DataObject)",
                nameof(specsDoJsonString));
        }

        var targetType = GetSpecDoType(dataType);

        // 处理Struct类型的特殊逻辑
        if (dataType == DataTypes.Struct)
        {
            return DeserializeStructSpecsDo(specsDoJsonString);
        }

        try
        {
            var result = TslSerializer.DeserializeObject(specsDoJsonString, targetType);
            return result as ISpecsDo ??
                throw new JsonSerializationException($"反序列化结果不是有效的 {nameof(ISpecsDo)} 实例");
        }
        catch (JsonSerializationException ex)
        {
            throw new JsonSerializationException(
                $"规格数据对象(DataObject)(SpecsDo)的 JOSN字符串反序列化失败！数据类型: {dataType}，目标类型: {targetType.Name}, SpecsParamJsion:{specsDoJsonString}",
                ex);
        }
    }

    /// <summary>
    /// 手动反序列化StructSpecsDo，逐个处理字段的SpecsDo
    /// </summary>
    private static StructSpecsDo DeserializeStructSpecsDo(string specsDoJsonString)
    {
        var dynamicStruct = TslSerializer.DeserializeDynamic(specsDoJsonString);
        var structSpecsDo = new StructSpecsDo();

        foreach (var field in dynamicStruct)
        {
            var structFieldDo = new StructFieldDo
            {
                Identifier = field.identifier.ToString(),
                Name = field.name.ToString(),
                DataType = Enum.Parse<DataTypes>(field.dataType.ToString(), true)  // true:参数忽略大小写
            };

            var specsDoJson = TslSerializer.SerializeObject(field.specsDo);
            structFieldDo.SpecsDo = DeserializeSpecsDo(structFieldDo.DataType, specsDoJson);

            structSpecsDo.Add(structFieldDo);
        }

        return structSpecsDo;
    }

    /// <summary>
    /// 验证数据类型<see cref="DataTypes"/>与规格数据对象(DataObject)类型<see cref="ISpecsDo"/>的匹配。
    /// 用于校验规格数据对象(DataObject)实例是否与数据类型匹配（如在 PropertyBuilder 中调用可提前发现类型错误）
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <param name="specsDo">规格数据对象(DataObject)实例</param>
    /// <exception cref="SpecsParamTypeMismatchException">类型不匹配时抛出</exception>
    public static void ValidateSpecsDoType(DataTypes dataType, ISpecsDo specsDo)
    {
        Check.NotNull(specsDo, nameof(specsDo));

        var expectedType = GetSpecDoType(dataType);
        var actualType = specsDo.GetType();

        if (expectedType != actualType)
        {
            throw new SpecsParamTypeMismatchException(
                dataType,
                actualType);
        }
    }
}