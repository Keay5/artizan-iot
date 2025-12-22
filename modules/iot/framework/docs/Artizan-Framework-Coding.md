# 物模型

## TSL

### 数据类型(DataType)

####  int32 (整数型) 

```json
"dataType": {
    "type": "int",
    "specs": {
        "min": "1",
        "max": "5",
        "unit": "gear",
        "unitName": "档",
        "step": "1"
    }
}
```



#### Float

```json
      "dataType": {
        "type": "float",
        "specs": {
          "min": "-10",
          "max": "60",
          "unit": "°",
          "unitName": "度",
          "step": "1"
        }
      }
```



#### Double

```json
                "dataType": {
                  "type": "float",
                  "specs": {
                    "min": "0",
                    "max": "3.4028235E38",
                    "unit": "Wh",
                    "unitName": "瓦时",
                    "step": "0.1"
                  }
                }
```



#### 布尔值

```json
"dataType": {
    "type": "bool",
    "specs": {
        "0": "关",
        "1": "开"
    }
}
```



#### 枚举

```json
"dataType": {
    "type": "enum",
    "specs": {
        "0": "正常",
        "1": "自然",
        "2": "睡眠",
        "3": "强力"
    }
}
```



#### 字符串

```json
"dataType": {
    "type": "text",
    "specs": {
        "length": "128"
    }
}
```



#### 时间

```json
"dataType": {
    "type": "date",
    "specs": {}
}
```



#### 数组

```json
"dataType": {
    "type": "array",
    "specs": {
        "size": "2",
        "item": {
            "type": "int"
        }
    }
}
```



#### Struct

```json
      "dataType": {
        "type": "struct",
        "specs": [
          {
            "identifier": "date",
            "name": "日期",
            "dataType": {
              "type": "date",
              "specs": {}
            }
          },
          {
            "identifier": "consumption",
            "name": "耗电量",
            "dataType": {
              "type": "float",
              "specs": {
                "min": "0",
                "max": "3.4028235E38",
                "unit": "Wh",
                "unitName": "瓦时",
                "step": "0.1"
              }
            }
          }
        ]
      }

```



# 单元测试

## 文献

ABP测试：

-  总揽：https://abp.io/docs/latest/testing/overall
- 单元测试：https://abp.io/docs/latest/testing/unit-tests
- 集成测试：
- UI 测试

ABP Cli: https://abp.io/docs/latest/cli

-  new-package: https://abp.io/docs/latest/cli#new-package



# Artizan.IoT.Abstractions.Tests

## 创建项目



```bash
\iot\framework> abp new-package --name Artizan.IoT.Abstractions.Tests --template lib.class-library --folder test
```

添加包

```xml
        <PackageReference Include="Volo.Abp.Core" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="NSubstitute.Analyzers.CSharp" >
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Shouldly" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.extensibility.execution" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Volo.Abp.Autofac" />
        <PackageReference Include="Volo.Abp.Guids"/>
        <PackageReference Include="Volo.Abp.TestBase" />

      <ProjectReference Include="..\..\src\Artizan.IoT.Abstractions\Artizan.IoT.Abstractions.csproj" />
      <ProjectReference Include="..\Artizan.IoT.TestBase\Artizan.IoT.TestBase.csproj" />
```



添加 DependsOn

```C#
[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpGuidsModule),
    typeof(IoTTestBaseModule),
    typeof(IoTAbstractionsModule)
)]
public class ArtizanIoTAbstractionsTestsModule : AbpModule
{

}
```



## 创建基础类

IoTAbstractionsTestBase

```C#
using Volo.Abp.Modularity;

namespace Artizan.IoT.Abstractions.Tests;
/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class IoTAbstractionsTestBase<TStartupModule> : IoTTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
```





## 添加测试用例

TLS_DataType_Tests.cs

```#
public class TLS_DataType_Tests
{
    [Fact]
    public void Deserialize_Int32Type_ReturnsCorrectSpecs()
    {
    	......
    }
    ......
}
```





## 测试资源管理器

在Visual Studio 中，打开顶部菜单栏【测试 > 测试资源管理器】,打开 【测试资源管理器】



# ThingModels

## Tsls

物联网（IoT）相关的设备模型定义框架，主要围绕 Thing Specification Language（TSL，设备规格语言）展开，用于构建和管理设备的属性、事件、服务等元数据模型。以下是该目录的主要内容和特点：

### 核心功能

1. **TSL 模型定义**：提供了一套完整的 TSL 模型结构，包含产品配置信息（`Profile`）、属性（`Property`）、事件（`Event`）和服务（`Service`）等核心元素，用于描述物联网设备的功能和特性。
2. **数据类型与规格**：定义了多种数据类型（如`Int32`、`Float`、`Boolean`、`Enum`、`Text`、`Date`、`Array`、`Struct`等），每种数据类型对应特定的规格（`Specs`），例如数值类型有`min`/`max`/`step`等规格，文本类型有`length`规格。
3. **构建器模式**：通过`PropertyBuilder`等构建器类，提供了简洁的 API 用于创建和配置设备属性，支持链式调用，方便设置数据类型、访问模式、描述等信息。
4. **类型转换与验证**：实现了规格转换器（`ISpecsConverter`），用于在数据对象（`ISpecsDo`）和规格对象（`ISpecs`）之间进行转换，同时具备类型验证功能，确保数据的合法性。
5. **动态管理**：TSL 模型支持动态管理内置服务和事件，例如当属性集合发生变化时，会自动同步`propertySet`、`propertyGet`等内置服务和`propertyPost`事件。

### 主要组件

1. **元数据类**：包括`Profile`（产品配置）、`Property`（属性）、`Event`（事件）、`Service`（服务）等，用于存储设备模型的基本信息。
2. **数据类型与规格接口**：`DataTypes`枚举定义了支持的数据类型，`ISpecs`和`ISpecsDo`接口分别约束规格对象和数据对象，各具体类型（如`NumericSpecs`、`KeyValueSpecs`）实现了这些接口。
3. **构建器**：`PropertyBuilder`用于便捷地构建属性实例，支持设置不同数据类型的规格。
4. **工厂类**：`TslFactory`提供了创建 TSL 模型元素（如属性、事件、服务）的方法。
5. **转换器工厂**：`SpecsConverterFactory`负责扫描和注册规格转换器，实现数据对象和规格对象的自动转换。

### 设计原则

1. **类型安全**：通过泛型和接口约束，确保不同数据类型的规格设置和转换符合预期，避免类型错误。
2. **可扩展性**：支持通过实现`ISpecs`、`ISpecsDo`和`ISpecsConverter`接口扩展新的数据类型和规格，无需修改原有代码。
3. **简洁易用**：采用构建器模式和工厂模式，简化了 TSL 模型的创建和配置过程，提高了代码的可读性和可维护性。
4. **动态适配**：根据属性的变化自动调整内置服务和事件，使模型能够动态适应设备功能的变更。

该项目适用于物联网平台中设备模型的定义和管理，为设备的接入、数据交互和功能调用提供了统一的元数据描述规范。



## 目前的设计问题

要降低该物联网设备模型定义框架的上手成本，可以从**简化使用流程、降低认知负荷、提供辅助工具与文档**等方面入手，具体措施如下：

### 1. 封装通用模板，减少重复配置

针对常见的设备类型（如传感器、控制器）提供**预定义模板**，避免用户从零开始构建 TSL 模型：

- 例如封装`SensorDeviceTemplate`（包含温度、湿度等标准属性）、`ControllerDeviceTemplate`（包含开关、模式等属性），用户可直接继承或修改模板，快速生成基础模型。

- 提供

  ```
  TSLBuilder
  ```

  工具类，封装高频操作（如创建标准属性、注册默认服务），例如：

  ```csharp
  // 简化API：一行代码创建标准温度属性
  var temperatureProp = TSLBuilder.CreateStandardProperty("temperature", DataTypes.Float, "温度", 0, 100);
  ```

  

### 2. 弱化设计模式的感知，提供 “傻瓜式” API

对底层复杂的策略模式、构建器模式进行封装，暴露更直观的**低代码 API**：

- 针对简单类型（如 Int32、Boolean）提供静态方法，替代链式构建器：

  ```csharp
  // 原复杂构建器
  var prop = new PropertyBuilder()
      .WithIdentifier("power")
      .WithDataType(DataTypes.Boolean)
      .WithBooleanSpecs(true)
      .Build();
  
  // 简化API
  var prop = PropertyHelper.CreateBooleanProperty("power", "电源状态", defaultValue: true);
  ```

- 隐藏`SpecsConverter`、`Strategy`等底层组件，默认自动注册常用转换器，用户无需关心类型映射逻辑。

  

### 3. 强化文档与示例，降低认知门槛

- 分层文档

  ：将文档分为「快速入门」「进阶用法」「底层原理」三层：

  - 「快速入门」提供 10 分钟上手教程，包含创建简单设备模型、生成 TSL JSON 等实操案例；
  - 「进阶用法」讲解复杂类型（Struct、Array）配置、自定义规格等场景；
  - 「底层原理」面向需要扩展框架的开发者，解释设计模式与接口设计。

- **交互式示例**：提供可运行的 Demo 项目，覆盖常见场景（如温湿度传感器、智能灯设备），用户可直接运行并修改参数，直观理解效果。

- **可视化工具**：开发简易的 TSL 可视化编辑器（Web / 桌面端），支持通过拖拽配置属性、事件、服务，自动生成代码或 JSON，减少手动编码。

### 4. 提供类型提示与错误引导

- 在 IDE 中增强

  智能提示

  通过 XML 注释详细说明每个 API 的用途、参数含义及示例，例如

  ```csharp
  /// <summary>
  /// 创建Int32类型属性
  /// </summary>
  /// <param name="identifier">属性标识符（唯一）</param>
  /// <param name="name">属性名称</param>
  /// <param name="min">最小值（示例：0）</param>
  /// <param name="max">最大值（示例：100）</param>
  /// <returns>Int32类型属性实例</returns>
  public static Property CreateInt32Property(string identifier, string name, int min, int max)
  ```

  

- **友好的错误提示**：在验证失败时，明确指出问题所在（如 “Int32 属性‘voltage’的 min 值（-10）不能小于 0”），并提供修复建议（如 “请调整 min 值为≥0”）。

### 5. 简化复杂类型配置

针对 Array、Struct 等复杂类型，提供**可视化配置工具**或**简化语法**：

- 例如配置 Struct 类型时，支持通过 JSON 直接定义结构，框架自动解析：

  ```csharp
  // 简化Struct配置
  var structProp = PropertyHelper.CreateStructProperty("location", "位置信息", 
      "{\"latitude\": {\"type\": \"Float\", \"min\": -90, \"max\": 90}, \"longitude\": {\"type\": \"Float\", \"min\": -180, \"max\": 180}}");
  ```

  

- 提供

  ```
  StructDefinition
  ```

  辅助类，通过链式调用定义 Struct 字段，替代手动嵌套构建：

  ```csharp
  var structDef = new StructDefinitionBuilder()
      .AddField("latitude", DataTypes.Float, 0, 90)
      .AddField("longitude", DataTypes.Float, 0, 180)
      .Build();
  var structProp = PropertyHelper.CreateStructProperty("location", "位置信息", structDef);
  ```

  

### 6. 提供调试与验证工具

- 内置**TSL 验证工具**：允许用户输入 TSL JSON 或代码，实时检查合法性并输出问题报告（如类型不匹配、规格缺失）。

- 日志增强：在关键步骤（如类型转换、模型构建）输出详细日志，帮助用户定位问题，例如：

  plaintext

  ```plaintext
  [INFO] 正在创建Int32属性"power"，规格：min=0, max=100
  [WARN] 属性"temp"的max值（50）小于min值（0），已自动修正为max=100
  ```

  

### 总结

通过**封装简化 API、提供模板与工具、强化文档与示例**，可大幅降低用户的初期学习成本，让新手快速聚焦业务逻辑而非框架细节；同时保留底层设计的灵活性，满足进阶用户的扩展需求。核心思路是：**让简单场景 “傻瓜化”，复杂场景 “可引导”**。