using Artizan.IoT.ThingModels.Tsls.Builders.Exceptions;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Volo.Abp;

namespace Artizan.IoT.ThingModels.Tsls.Builders;

public class PropertyBuilder
{
    private bool _isBuilt;

    // 基础属性（不可变）
    public string Identifier { get; }
    public string Name { get; }
    public AccessModes AccessMode { get; }
    public bool Required { get; }
    public DataTypes DataType { get; }
    public ISpecsDo SpecsDo { get; private set; }
    public string? Description { get; private set; }

    #region 类型映射表

    // 数据类型与规格数据对象(DataObject)类型的映射关系
    private static readonly Dictionary<DataTypes, Type> DataTypeToSpecsParamTypeMap = new()
    {
        { DataTypes.Int32, typeof(NumericSpecsDo) },
        { DataTypes.Float, typeof(NumericSpecsDo) },
        { DataTypes.Double, typeof(NumericSpecsDo) },
        { DataTypes.Text, typeof(StringSpecsDo) },
        { DataTypes.Boolean, typeof(KeyValueSpecsDo) },
        { DataTypes.Enum, typeof(KeyValueSpecsDo) },
        { DataTypes.Array, typeof(ArraySpecsDo) },
        { DataTypes.Struct, typeof(StructSpecsDo) },
        { DataTypes.Date, typeof(EmptySpecsDo) }
    };
    #endregion

    /// <summary>
    /// 构造函数（全参数校验）
    /// </summary>
    public PropertyBuilder(
        string identifier,
        string name,
        AccessModes accessMode,
        bool required,
        DataTypes dataType,
        string? description = null)
    {

        // TODO: 重构：移除这些验证到 Property 内部去。Property 外部只管赋值即可。
        Identifier = ValidateIdentifier(identifier);
        Name = ValidateName(name);
        AccessMode = accessMode;
        Required = required;
        DataType = ValidateDataType(dataType);
        Description = description;
    }

    #region 核心方法
    /// <summary>
    /// 统一规格数据对象(DataObject)设置入口（所有类型复用）
    /// </summary>
    public PropertyBuilder WithSpecsDo(ISpecsDo specsDo)
    {
        Check.NotNull(specsDo, nameof(specsDo));

        ValidateNotBuilt();
        ValidateSpecsParamType(specsDo);
        ValidateSpecsDo(specsDo);

        SpecsDo = specsDo;
        return this;
    }

    /// <summary>
    /// 设置描述信息
    /// </summary>
    public PropertyBuilder WithDescription(string? description)
    {
        ValidateNotBuilt();
        Description = description;
        return this;
    }

    /// <summary>
    /// 构建最终的Property实例对象（最终校验）
    /// </summary>
    public Property Build()
    {
        ValidateNotBuilt();
        _isBuilt = true;

        if (DataType != DataTypes.Date)
        {
            Check.NotNull(SpecsDo, nameof(SpecsDo));
        }

        return new Property
        {
            Identifier = Identifier,
            Name = Name,
            Desc = Description,
            AccessMode = AccessMode,
            Required = Required,
            DataType = new DataType
            {
                Type = DataType,
                Specs = ConvertSpecsDoToSpecs()
            }
        };
    }

    #endregion

    #region 转换逻辑：SpecsDo 转化为 Specs

    /// <summary>
    /// SpecsDo 转化为 Specs
    /// 例如： 将 NumericSpecsDo 转为 NumericSpecs
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private ISpecs? ConvertSpecsDoToSpecs()
    {
        // 为 Date 类型自动填充默认的 EmptySpecsDo
        if (DataType == DataTypes.Date && SpecsDo == null)
        {
            SpecsDo = new EmptySpecsDo();
        }

        if (SpecsDo == null)
        {
            return null;
        }

        // 根据当前数据类型获取转换器
        var specsConverter = SpecsConverterFactory.GetSpecsConverter(DataType);
        return specsConverter.Convert(SpecsDo);
    }

    #endregion

    #region 校验方法

    private string ValidateIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("属性标识不能为空或空白");
        }
        if (!Regex.IsMatch(identifier, "^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException("属性标识只能包含字母、数字和下划线");
        }

        return identifier;
    }

    private string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("属性名称不能为空或空白");
        }
        return name;
    }

    private DataTypes ValidateDataType(DataTypes dataType)
    {
        if (!Enum.IsDefined(typeof(DataTypes), dataType))
        {
            throw new ArgumentOutOfRangeException(nameof(dataType), "无效的数据类型");
        }

        return dataType;
    }

    private void ValidateNotBuilt()
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException("已完成构建，不能再修改");
        }
    }

    private void ValidateSpecsParamType(ISpecsDo specsDo)
    {
        if (!DataTypeToSpecsParamTypeMap.TryGetValue(DataType, out var expectedSpecsParamType))
        {
            throw new NotSupportedException($"数据类型[{DataType}]未配置对应的规格数据对象(DataObject)类型");
        }

        // 校验：传入与数据类型是否匹配的规格数据对象(DataObject)，例如：Int32 预期 NumericSpecsDo，实际传 StringSpecsDo。
        if (specsDo.GetType() != expectedSpecsParamType)
        {
            throw new SpecsParamTypeMismatchException(DataType, specsDo.GetType());
        }
    }

    private void ValidateSpecsDo(ISpecsDo specsDo)
    {
        switch (specsDo)
        {
            case NumericSpecsDo numericSpecsDo:
                ValidateNumericParams(numericSpecsDo);
                break;
            case KeyValueSpecsDo keyValueSpecsDo:
                ValidateKeyValueParams(keyValueSpecsDo);
                break;
            case StringSpecsDo stringSpecsDo:
                ValidateStringParams(stringSpecsDo);
                break;
            case ArraySpecsDo arraySpecsDo:
                ValidateArrayParams(arraySpecsDo);
                break;
            case StructSpecsDo structSpecsDo:
                ValidateStructParams(structSpecsDo);
                break;
            case EmptySpecsDo _:
                break;
            default:
                throw new UnsupportedSpecsTypeException(specsDo.GetType());
        }
    }

    private void ValidateNumericParams(NumericSpecsDo specsDo)
    {
        if (specsDo.Min != null && specsDo.Max != null)
        {
            if (!double.TryParse(specsDo.Min, out var min) || !double.TryParse(specsDo.Max, out var max))
            {
                throw new FormatException("数值参数Min/Max必须是有效的数字格式");
            }

            if (min > max)
            {
                throw new InvalidOperationException($"数值参数Min[{specsDo.Min}]不能大于Max[{specsDo.Max}]");
            }
        }
    }

    private void ValidateKeyValueParams(KeyValueSpecsDo specsDo)
    {
        if (specsDo.Values == null || specsDo.Values.Count == 0)
        {
            throw new ArgumentException("键值对不能为空");
        }
        if (specsDo.Values.Any(kv => string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value)))
        {
            throw new ArgumentException("键值对的键和值不能为空");
        }
    }

    private void ValidateStringParams(StringSpecsDo specsDo)
    {
        if (string.IsNullOrEmpty(specsDo.Length) || !int.TryParse(specsDo.Length, out var length) || length <= 0)
        {
            throw new ArgumentException("字符串长度必须是正整数");
        }
    }

    private void ValidateArrayParams(ArraySpecsDo specsDo)
    {
        if (specsDo.Size == null)
        {
            throw new ArgumentNullException(nameof(specsDo.Size), "数组大小不能为null");
        }

        if (string.IsNullOrWhiteSpace(specsDo.Size) || !int.TryParse(specsDo.Size, out var size) || size <= 0)
        {
            throw new ArgumentException("数组大小必须是正整数", nameof(specsDo.Size));
        }

        if (specsDo.ItemType == DataTypes.Array || specsDo.ItemType == DataTypes.Struct)
        {
            throw new NotSupportedException("暂不支持嵌套数组/结构体");
        }

        if (specsDo.ItemSpecs == null && specsDo.ItemType != DataTypes.Date)
        {
            throw new ArgumentException($"数组元素类型[{specsDo.ItemType}]需配置规格数据对象(DataObject)", nameof(specsDo.ItemSpecs));
        }
    }

    private void ValidateStructParams(StructSpecsDo specsDo)
    {
        if (specsDo == null || specsDo.Count == 0)
        {
            throw new InvalidOperationException("结构体字段不能为空");
        }
    }

    #endregion

}