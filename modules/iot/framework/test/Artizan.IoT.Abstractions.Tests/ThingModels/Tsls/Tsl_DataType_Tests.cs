using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Artizan.IoT.Abstractions.Tests.ThingModels.Tsls;

public class Tsl_DataType_Tests
{
    [Fact]
    public void Deserialize_Int32Type_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""int"",""specs"":{""min"":""1"",""max"":""5"",""unit"":""gear"",""unitName"":""档"",""step"":""1""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Int32, dataType.Type);
        var specs = Assert.IsType<NumericSpecs>(dataType.Specs);
        Assert.Equal("1", specs.Min);
        Assert.Equal("5", specs.Max);
        Assert.Equal(1, specs.GetValue<int>("step"));
    }

    [Fact]
    public void Deserialize_FloatType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""float"",""specs"":{""min"":""1.0"",""max"":""10.0"",""unit"":""kg"",""unitName"":""千克"",""step"":""0.5""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Float, dataType.Type);
        var specs = Assert.IsType<NumericSpecs>(dataType.Specs);
        Assert.Equal("1.0", specs.Min);
        Assert.Equal("10.0", specs.Max);
        Assert.Equal(0.5, specs.GetValue<float>("step"));
    }

    [Fact]
    public void Deserialize_DoubleType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""double"",""specs"":{""min"":""1.0"",""max"":""10.0"",""unit"":""kg"",""unitName"":""千克"",""step"":""0.5""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Double, dataType.Type);
        var specs = Assert.IsType<NumericSpecs>(dataType.Specs);
        Assert.Equal("1.0", specs.Min);
        Assert.Equal("10.0", specs.Max);
        Assert.Equal(0.5, specs.GetValue<double>("step"));
    }

    [Fact]
    public void Deserialize_BoolType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""bool"",""specs"":{""0"":""关"",""1"":""开""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Boolean, dataType.Type);
        var specs = Assert.IsType<KeyValueSpecs>(dataType.Specs);
        Assert.Equal("关", specs.GetValue<string>("0"));
        Assert.Equal("开", specs.GetValue<string>("1"));
    }

    [Fact]
    public void Deserialize_EnumType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""enum"",""specs"":{""0"":""正常"",""1"":""自然"",""2"":""睡眠""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Enum, dataType.Type);
        var specs = Assert.IsType<KeyValueSpecs>(dataType.Specs);
        Assert.Equal(3, specs.Values.Count);
        Assert.Equal("睡眠", specs.GetValue<string>("2"));
    }

    [Fact]
    public void Deserialize_StringType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""text"",""specs"":{""length"":""128""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Text, dataType.Type);
        var specs = Assert.IsType<StringSpecs>(dataType.Specs);
        Assert.Equal("128", specs.Length);
        Assert.Equal(128, specs.GetValue<int>("length"));
    }

    [Fact]
    public void Deserialize_DateType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""date"",""specs"":{}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Date, dataType.Type);
        Assert.IsType<EmptySpecs>(dataType.Specs);
    }

    [Fact]
    public void Deserialize_ArrayType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{""type"":""array"",""specs"":{""size"":""2"",""item"":{""type"":""int""}}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Array, dataType.Type);
        var specs = Assert.IsType<ArraySpecs>(dataType.Specs);
        Assert.Equal("2", specs.Size);
        Assert.Equal(DataTypes.Int32, specs.Item.Type);
        Assert.Equal(2, specs.GetValue<int>("size"));
    }

    [Fact]
    public void Deserialize_StructType_ReturnsCorrectSpecs()
    {
        // Arrange
        var json = @"{
                ""type"":""struct"",
                ""specs"":[
                    {""identifier"":""date"",""name"":""日期"",""dataType"":{""type"":""date"",""specs"":{}}},
                    {""identifier"":""consumption"",""name"":""耗电量"",""dataType"":{""type"":""float"",""specs"":{""min"":""0""}}}
                ]
            }";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(json);

        // Assert
        Assert.Equal(DataTypes.Struct, dataType.Type);
        var specs = Assert.IsType<StructSpecs>(dataType.Specs);
        Assert.Equal(2, specs.Count);
        Assert.Equal("date", specs[0].Identifier);
        Assert.Equal("耗电量", specs[1].Name);
        Assert.Equal(DataTypes.Float, specs[1].DataType.Type);
    }

    [Fact]
    public void Serialize_Int32Type_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""int"",""specs"":{""min"":""1"",""max"":""5"",""step"":""1"",""unit"":""gear"",""unitName"":""档""}}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void Serialize_DoubleType_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""double"",""specs"":{""min"":""1.75"",""max"":""3000.25"",""step"":""0.5"",""unit"":""ka"",""unitName"":""千克""}}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void Serialize_FloatType_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""float"",""specs"":{""min"":""1.75"",""max"":""3000.25"",""step"":""0.5"",""unit"":""ka"",""unitName"":""千克""}}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void Serialize_BoolType_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""bool"",""specs"":{""0"":""关"",""1"":""开""}}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void Serialize_ArrayType_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""array"",""specs"":{""size"":""2"",""item"":{""type"":""int""}}}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void Serialize_StructType_ExactMatchOriginalJson()
    {
        // Arrange
        var originalJson = @"{""type"":""struct"",""specs"":[{""identifier"":""date"",""name"":""日期"",""dataType"":{""type"":""date"",""specs"":{}}},{""identifier"":""consumption"",""name"":""耗电量"",""dataType"":{""type"":""float"",""specs"":{""min"":""0"",""max"":""3.4028235E38"",""step"":""0.1"",""unit"":""Wh"",""unitName"":""瓦时""}}}]}";
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);

        // Act
        var serializedJson = TslSerializer.SerializeObject(dataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
    }

    [Fact]
    public void RoundTrip_Serialization_ExactMatch()
    {
        // Arrange
        var originalJson = @"{""type"":""enum"",""specs"":{""0"":""正常"",""1"":""自然"",""2"":""睡眠"",""3"":""强力""}}";

        // Act
        var dataType = TslSerializer.DeserializeObject<DataType>(originalJson);
        var serializedJson = TslSerializer.SerializeObject(dataType);
        var roundTripDataType = TslSerializer.DeserializeObject<DataType>(serializedJson);
        var roundTripJson = TslSerializer.SerializeObject(roundTripDataType);

        // Assert
        Assert.Equal(originalJson, serializedJson);
        Assert.Equal(serializedJson, roundTripJson);
    }

    [Fact]
    public void GetValue_InvalidKey_ThrowsException()
    {
        // Arrange
        var specs = new NumericSpecs(min: "1", max: "100");

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => specs.GetValue<int>("InvalidKey"));
    }

    [Fact]
    public void SetValue_UpdatesSpecsCorrectly()
    {
        // Arrange
        var specs = new NumericSpecs(min:"1", max:"100");

        // Act
        specs.SetValue("min", 10);
        specs.SetValue("unit", "kg");

        // Assert
        Assert.Equal("10", specs.Min);
        Assert.Equal("kg", specs.Unit);
    }

    [Fact]
    public void Deserialize_UnsupportedType_ReturnNull()
    {
        // Arrange
        var json = @"{""type"":""unknown"",""specs"":{}}";

        // Act & Assert
        //Assert.Throws<JsonException>(() => TslSerializer.DeserializeObject<DataType>(json));
        var result = TslSerializer.DeserializeObject<DataType>(json);
        result.ShouldBeNull();
    }
}