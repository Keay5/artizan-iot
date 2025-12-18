using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Properties;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;
using Xunit;
using Shouldly;
using System.Linq;
using System;

namespace Artizan.IoT.Abstractions.Tests.ThingModels.Tsls;

public class Tsl_Tests
{
    #region 测试基础配置
    private readonly string _productKey = "testProductKey";
    private readonly string _moduleId = "testModule";
    private readonly string _moduleName = "测试模块";
    private readonly string _version = "1.0";
    private readonly string _description = "测试描述";

    private Tsl CreateEmptyTsl() =>
        new Tsl(_productKey, _moduleId, _moduleName, true, _version, _description);

    private Property CreateReadWriteProperty(string identifier = "temperature") =>
        new Property
        {
            Identifier = identifier,
            Name = $"{identifier}名称",
            AccessMode = AccessModes.ReadAndWrite,
            DataType = new DataType { Type = DataTypes.Float },
            Required = true
        };

    private Property CreateReadOnlyProperty(string identifier = "humidity") =>
        new Property
        {
            Identifier = identifier,
            Name = $"{identifier}名称",
            AccessMode = AccessModes.ReadOnly,
            DataType = new DataType { Type = DataTypes.Int32 },
            Required = false
        };
    #endregion

    #region 初始化测试
    [Fact]
    public void Constructor_ShouldInitializeBasicProperties()
    {
        // Act
        var tsl = CreateEmptyTsl();

        // Assert
        tsl.Profile.ShouldNotBeNull();
        tsl.Profile.ProductKey.ShouldBe(_productKey);
        tsl.Profile.Version.ShouldBe(_version);
        tsl.FunctionBlockId.ShouldBe(_moduleId);
        tsl.FunctionBlockName.ShouldBe(_moduleName);
        tsl.IsDefault.ShouldBeTrue();
        tsl.Description.ShouldBe(_description);

        // 初始状态无服务/事件
        tsl.Services.ShouldNotBeNull();
        tsl.Services.ShouldBeEmpty();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.ShouldBeEmpty();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.ShouldBeEmpty();
    }
    #endregion

    #region 属性添加测试
    [Fact]
    public void AddProperty_ShouldSyncServicesAndEvents()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        var rwProp = CreateReadWriteProperty();

        // Act
        tsl.AddProperty(rwProp);

        // Assert：服务自动创建
        tsl.Services.ShouldContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);
        tsl.Services.ShouldContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertyGet);
        tsl.Events.ShouldContain(e => e.Method == EventMethodGenerator.BuiltInPropertyPost);

        // Assert：参数同步
        var setService = tsl.Services.First(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);
        setService.InputData.Count.ShouldBe(1);
        setService.InputData[0].Identifier.ShouldBe("temperature");
    }

    [Fact]
    public void DirectAddToProperties_ShouldTriggerSync()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        var roProp = CreateReadOnlyProperty();

        // Act：直接操作Properties集合
        tsl.Properties.Add(roProp);

        // Assert
        tsl.Services.ShouldContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertyGet);
        tsl.Services.ShouldNotContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);
        tsl.Events.ShouldContain(e => e.Method == EventMethodGenerator.BuiltInPropertyPost);
    }
    #endregion

    #region 属性移除测试
    [Fact]
    public void RemoveProperty_ShouldCleanupServices()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        tsl.AddProperty(CreateReadWriteProperty());

        // Act
        tsl.RemoveProperty("temperature");

        // Assert
        tsl.Services.ShouldBeEmpty();
        tsl.Events.ShouldBeEmpty();
    }

    [Fact]
    public void DirectRemoveFromProperties_ShouldCleanupEvents()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        var prop = CreateReadOnlyProperty();
        tsl.Properties.Add(prop);

        // Act
        tsl.Properties.Remove(prop);

        // Assert
        tsl.Events.ShouldBeEmpty();
    }

    [Fact]
    public void ClearProperties_ShouldRemoveAllServicesAndEvents()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        tsl.Properties.Add(CreateReadWriteProperty());
        tsl.Properties.Add(CreateReadOnlyProperty());

        // Act
        tsl.Properties.Clear();

        // Assert
        tsl.Services.ShouldBeEmpty();
        tsl.Events.ShouldBeEmpty();
    }
    #endregion

    #region 服务同步规则测试
    [Fact]
    public void PropertySetService_OnlyCreatedForReadWriteProperties()
    {
        // Arrange
        var tsl = CreateEmptyTsl();

        // Act1：添加只读属性
        tsl.AddProperty(CreateReadOnlyProperty());

        // Assert1：无propertySet服务
        tsl.Services.ShouldNotContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);

        // Act2：添加读写属性
        tsl.AddProperty(CreateReadWriteProperty());

        // Assert2：创建propertySet服务
        tsl.Services.ShouldContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);
    }

    [Fact]
    public void PropertyGetService_IncludesAllReadableProperties()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        tsl.AddProperty(CreateReadOnlyProperty("roProp"));
        tsl.AddProperty(CreateReadWriteProperty("rwProp"));

        // Act
        var getService = tsl.Services.First(s => s.Method == ServiceMethodGenerator.BuiltInPropertyGet);

        // Assert
        getService.InputData.Count.ShouldBe(2);
        getService.InputData.Select(p => p.Identifier).ShouldContain("roProp");
        getService.InputData.Select(p => p.Identifier).ShouldContain("rwProp");
    }
    #endregion

    #region 异常测试
    [Fact]
    public void AddProperty_NullProperty_ThrowsArgumentNullException()
    {
        // Arrange
        var tsl = CreateEmptyTsl();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => tsl.AddProperty(null))
            .ParamName.ShouldBe("property");
    }

    [Fact]
    public void RemoveProperty_EmptyIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var tsl = CreateEmptyTsl();

        // Act & Assert
        Should.Throw<ArgumentException>(() => tsl.RemoveProperty(""))
            .ParamName.ShouldBe("identifier");
    }

    [Fact]
    public void RemoveProperty_NonExistentIdentifier_ReturnsFalse()
    {
        // Arrange
        var tsl = CreateEmptyTsl();

        // Act
        var result = tsl.RemoveProperty("nonExistent");

        // Assert
        result.ShouldBeFalse();
    }
    #endregion

    #region 批量操作测试
    [Fact]
    public void SetProperties_ReplacesAndSyncs()
    {
        // Arrange
        var tsl = CreateEmptyTsl();
        tsl.AddProperty(CreateReadWriteProperty("oldProp"));

        var newProps = new[] { CreateReadOnlyProperty("newProp") };

        // Act
        tsl.SetProperties(newProps);

        // Assert
        tsl.Properties.Count.ShouldBe(1);
        tsl.Properties[0].Identifier.ShouldBe("newProp");
        tsl.Services.ShouldContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertyGet);
        tsl.Services.ShouldNotContain(s => s.Method == ServiceMethodGenerator.BuiltInPropertySet);
    }
    #endregion
}