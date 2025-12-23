using Artizan.IoT.ThingModels.Tsls.Builders.Exceptions;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using System;
using System.Collections.Generic;
using Xunit;

namespace Artizan.IoT.Abstractions.Tests.ThingModels.Tsls;

public class TslFactory_Property_Tests
{
    // 通用测试参数
    private const string ValidIdentifier = "test_prop";
    private const string ValidName = "测试属性";
    private const AccessModes AccessMode = AccessModes.ReadAndWrite;
    private const bool Required = true;
    private const string Description = "测试描述";

    #region CreateProperty 方法测试（全类型覆盖）

    [Fact]
    public void CreateProperty_Int32Type_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new NumericSpecsDo { Min = "0", Max = "100", Step = "1" };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Int32, specsDo, Description);

        // Assert
        Assert.NotNull(property);
        Assert.Equal(ValidIdentifier, property.Identifier);
        Assert.Equal(DataTypes.Int32, property.DataType.Type);
        Assert.IsType<NumericSpecs>(property.DataType.Specs);
        var specs = (NumericSpecs)property.DataType.Specs;
        Assert.Equal("0", specs.Min);
    }

    [Fact]
    public void CreateProperty_FloatType_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new NumericSpecsDo { Min = "0.0", Max = "100.0", Step = "0.1" };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Float, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Float, property.DataType.Type);
        Assert.IsType<NumericSpecs>(property.DataType.Specs);
    }

    [Fact]
    public void CreateProperty_BooleanType_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new KeyValueSpecsDo
        {
            Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" } }
        };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Boolean, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Boolean, property.DataType.Type);
        Assert.IsType<KeyValueSpecs>(property.DataType.Specs);
        Assert.Equal(2, ((KeyValueSpecs)property.DataType.Specs).Values.Count);
    }

    [Fact]
    public void CreateProperty_EnumType_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new KeyValueSpecsDo
        {
            Values = new Dictionary<string, string> { { "0", "低" }, { "1", "中" }, { "2", "高" } }
        };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Enum, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Enum, property.DataType.Type);
        Assert.IsType<KeyValueSpecs>(property.DataType.Specs);
    }

    [Fact]
    public void CreateProperty_TextType_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new StringSpecsDo { Length = "128" };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Text, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Text, property.DataType.Type);
        Assert.IsType<StringSpecs>(property.DataType.Specs);
        Assert.Equal("128", ((StringSpecs)property.DataType.Specs).Length);
    }

    [Fact]
    public void CreateProperty_DateType_ReturnsValidProperty()
    {
        // Arrange
        var specsDo = new EmptySpecsDo();

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Date, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Date, property.DataType.Type);
        Assert.IsType<EmptySpecs>(property.DataType.Specs);
    }

    [Fact]
    public void CreateProperty_ArrayType_ReturnsValidProperty()
    {
        // Arrange
        var itemParams = new NumericSpecsDo { Min = "0", Max = "5" };
        var specsDo = new ArraySpecsDo
        {
            Size = "3",
            ItemType = DataTypes.Int32,
            ItemSpecs = itemParams
        };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Array, specsDo, Description);

        // Assert
        Assert.Equal(DataTypes.Array, property.DataType.Type);
        var arraySpecs = (ArraySpecs)property.DataType.Specs;
        Assert.Equal("3", arraySpecs.Size);
        Assert.Equal(DataTypes.Int32, arraySpecs.Item.Type);
        Assert.IsType<NumericSpecs>(arraySpecs.Item.Specs);
    }

    [Fact]
    public void CreateProperty_StructType_ReturnsValidProperty()
    {
        // Arrange
        var structParams = new StructSpecsDo
        {
            new StructFieldDo()
            {
                Identifier = "field1",
                Name = "字段1",
                DataType = DataTypes.Int32,
                SpecsDo = new NumericSpecsDo { Min = "0" }
            }
        };

        // Act
        var property = TslFactory.CreateProperty(
            ValidIdentifier, ValidName, AccessMode, Required,
            DataTypes.Struct, structParams, Description);

        // Assert
        Assert.Equal(DataTypes.Struct, property.DataType.Type);
        var structSpecs = (StructSpecs)property.DataType.Specs;

        // 直接通过集合特性断言（StructSpecs 是 List<StructField>）
        Assert.Single(structSpecs); // 验证集合元素数量
        Assert.Equal("field1", structSpecs[0].Identifier); // 直接索引访问元素
        Assert.Equal("字段1", structSpecs[0].Name);
        Assert.Equal(DataTypes.Int32, structSpecs[0].DataType.Type);
    }

    [Fact]
    public void CreateProperty_InvalidSpecsDo_ThrowsException()
    {
        // 传入与数据类型不匹配的规格参数（Int32预期NumericSpecsDo，实际传StringSpecsDo）
        var invalidParams = new StringSpecsDo();

        // Act & Assert
        Assert.Throws<SpecsParamTypeMismatchException>(() =>
            TslFactory.CreateProperty(
                ValidIdentifier, ValidName, AccessMode, Required,
                DataTypes.Int32, invalidParams, Description));
    }

    #endregion

    #region CreatePropertyBuilder 方法测试（全类型覆盖）

    [Fact]
    public void CreatePropertyBuilder_Int32Type_BuildsValidProperty()
    {
        // Act
        var property = TslFactory.CreatePropertyBuilder(
                ValidIdentifier, ValidName, AccessMode, Required, DataTypes.Int32)
            .WithSpecsDo(new NumericSpecsDo
            {
                Min = "0",
                Max = "100",
                Step = "1"
            })
            .WithDescription(Description)
            .Build();

        // Assert
        Assert.Equal(DataTypes.Int32, property.DataType.Type);
        Assert.IsType<NumericSpecs>(property.DataType.Specs);
    }

    [Fact]
    public void CreatePropertyBuilder_ArrayType_BuildsValidProperty()
    {
        // Act
        var property = TslFactory.CreatePropertyBuilder(
                ValidIdentifier, ValidName, AccessMode, Required, DataTypes.Array)
            .WithSpecsDo(new ArraySpecsDo
            {
                Size = "5",
                ItemType = DataTypes.Text,
                ItemSpecs = new StringSpecsDo { Length = "50" }
            })
            .Build();

        // Assert
        var arraySpecs = (ArraySpecs)property.DataType.Specs;
        Assert.Equal("5", arraySpecs.Size);
        Assert.Equal(DataTypes.Text, arraySpecs.Item.Type);
        Assert.Equal("50", ((StringSpecs)arraySpecs.Item.Specs).Length);
    }

    [Fact]
    public void CreatePropertyBuilder_StructType_BuildsValidProperty()
    {
        // Act
        var property = TslFactory.CreatePropertyBuilder(
                ValidIdentifier, ValidName, AccessMode, Required, DataTypes.Struct)
            .WithSpecsDo(new StructSpecsDo()
            {
                new()
                {
                    Identifier = "field1",
                    Name = "字段1",
                    DataType = DataTypes.Int32,
                    SpecsDo = new NumericSpecsDo { Min = "0" }
                },
                new StructFieldDo()
                {
                    Identifier = "subProp",
                    Name = "子属性",
                    DataType = DataTypes.Boolean,
                    SpecsDo = new KeyValueSpecsDo
                    {
                        Values = new Dictionary<string, string> { { "0", "否" }, { "1", "是" } }
                    }
                }
            }
            //Fields.Add(new StructFieldDo
            //{
            //    Identifier = "subProp",
            //    Name = "子属性",
            //    DataType = DataTypes.Boolean,
            //    SpecsDo = new KeyValueSpecsDo
            //    {
            //        Values = new Dictionary<string, string> { { "0", "否" }, { "1", "是" } }
            //    }
            //});
            )
            .Build();

        // Assert
        var structSpecs = (StructSpecs)property.DataType.Specs;
        Assert.Equal(DataTypes.Boolean, structSpecs[1].DataType.Type);
    }

    [Fact]
    public void CreatePropertyBuilder_MissingSpecsDo_ThrowsException()
    {
        // 未设置规格参数（非Date类型）
        var builder = TslFactory.CreatePropertyBuilder(
            ValidIdentifier, ValidName, AccessMode, Required, DataTypes.Text);
        Assert.Throws<ArgumentNullException>(() => builder.Build());

        // Act & Assert
    }

    [Fact]
    public void CreatePropertyBuilder_DateType_WithoutSpecsDo_BuildsSuccessfully()
    {
        // Date类型允许空规格参数
        var property = TslFactory.CreatePropertyBuilder(
                ValidIdentifier, ValidName, AccessMode, Required, DataTypes.Date)
            .Build();

        // Assert
        Assert.NotNull(property);
        Assert.IsType<EmptySpecs>(property.DataType.Specs);
    }

    #endregion
}