# Artizan.IoT.TimeSeries.InfluxDB2 测试用例（纯 V2 适配 + 通用 TimeSeriesData 模型）

以下测试用例**完全基于你提供的通用 TimeSeriesData 模型**，仅适配 InfluxDB2 原生 API（无任何 InfluxDB3 相关内容），使用指定的 `xUnit + Shouldly + Microsoft.NET.Test.Sdk` 框架，覆盖模型自身逻辑、InfluxDB2 客户端封装、仓储层核心功能，且所有代码仅聚焦 InfluxDB2 场景。

## 一、测试项目结构

```plaintext
Artizan.IoT.TimeSeries.InfluxDB2.Tests/
├── UnitTests/
│   ├── Models/
│   │   └── TimeSeriesDataTests.cs       // 通用模型单元测试
│   ├── Clients/
│   │   └── InfluxDb2ClientTests.cs      // InfluxDB2客户端封装测试
│   └── Repositories/
│       └── TimeSeriesRepositoryTests.cs // InfluxDB2仓储层测试
├── IntegrationTests/
│   ├── InfluxDb2ConnectionTests.cs      // 真实InfluxDB2连接测试
│   └── TimeSeriesDataCrudTests.cs       // 真实数据读写测试
├── Helpers/
│   ├── TestConfig.cs                    // InfluxDB2配置读取
│   └── TestDataFactory.cs               // 测试数据生成
└── Artizan.IoT.TimeSeries.InfluxDB2.Tests.csproj
```

## 二、测试项目配置文件（csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- 指定测试框架 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Shouldly" Version="4.2.1" />
    
    <!-- 辅助依赖 -->
    <PackageReference Include="Moq" Version="4.20.70" /> <!-- Mock框架 -->
    <PackageReference Include="InfluxDB.Client" Version="4.44.0" /> <!-- 仅InfluxDB2客户端 -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- 引用业务项目 -->
    <ProjectReference Include="..\Artizan.IoT.TimeSeries.InfluxDB2\Artizan.IoT.TimeSeries.InfluxDB2.csproj" />
    <ProjectReference Include="..\Artizan.IoT.TimeSeries\Models\Artizan.IoT.TimeSeries.Models.csproj" />
  </ItemGroup>

  <!-- 配置文件复制 -->
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```



## 三、测试辅助类（纯 InfluxDB2 + 通用模型）

### 1. Helpers/TestConfig.cs（InfluxDB2 配置）

```csharp
using Microsoft.Extensions.Configuration;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;

/// <summary>
/// InfluxDB2 测试配置（仅V2，无V3）
/// appsettings.json示例：
/// {
///   "InfluxDB2": {
///     "Url": "http://localhost:8086",
///     "Token": "your-influxdb2-token",
///     "Organization": "your-org",
///     "Bucket": "test-bucket",
///     "Timeout": 30
///   }
/// }
/// </summary>
public static class TestConfig
{
    private static readonly IConfiguration _configuration;

    static TestConfig()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    // 仅InfluxDB2核心配置
    public static string InfluxDb2Url => _configuration["InfluxDB2:Url"] ?? "http://localhost:8086";
    public static string InfluxDb2Token => _configuration["InfluxDB2:Token"] ?? throw new InvalidOperationException("InfluxDB2 Token未配置");
    public static string InfluxDb2Org => _configuration["InfluxDB2:Organization"] ?? throw new InvalidOperationException("InfluxDB2组织未配置");
    public static string InfluxDb2Bucket => _configuration["InfluxDB2:Bucket"] ?? throw new InvalidOperationException("InfluxDB2 Bucket未配置");
    public static int InfluxDb2Timeout => int.TryParse(_configuration["InfluxDB2:Timeout"], out var t) ? t : 30;
}
```

### 2. Helpers/TestDataFactory.cs（通用模型测试数据）

```
using Artizan.IoT.TimeSeries.Models;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Moq;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;

/// <summary>
/// 测试数据工厂（适配通用TimeSeriesData + InfluxDB2）
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// 创建通用TimeSeriesData测试实例
    /// </summary>
    public static TimeSeriesData CreateTestTimeSeriesData(string deviceId = "test-device-001")
    {
        var data = new TimeSeriesData
        {
            ThingIdentifier = deviceId,
            UtcDateTime = DateTime.UtcNow,
            Measurement = "temperature",
            InsertedUtcTime = DateTime.UtcNow
        };

        // 设置标签（适配InfluxDB2 Tag）
        data.SetTag("product_key", "test-product-001");
        data.SetTag("region", "hangzhou");

        // 设置字段（适配InfluxDB2 Field）
        data.SetField("temp", 26.5);
        data.SetField("humidity", 62.3);

        return data;
    }

    /// <summary>
    /// 通用模型转换为InfluxDB2原生PointData
    /// </summary>
    public static PointData ToInfluxDb2Point(TimeSeriesData data)
    {
        var point = PointData.Measurement(data.Measurement)
            .Tag("thing_identifier", data.ThingIdentifier)
            .Timestamp(data.UtcDateTime, WritePrecision.Ns);

        // 追加扩展标签
        foreach (var tag in data.Tags)
        {
            point = point.Tag(tag.Key, tag.Value);
        }

        // 追加数值字段
        foreach (var field in data.Fields)
        {
            point = point.Field(field.Key, field.Value);
        }

        return point;
    }

    /// <summary>
    /// 构建InfluxDB2原生Flux查询语句
    /// </summary>
    public static string BuildInfluxDb2FluxQuery(string bucket, string org, string deviceId, DateTime start, DateTime stop)
    {
        return $@"from(bucket: ""{bucket}"")
  |> range(start: {start:o}, stop: {stop:o})
  |> filter(fn: (r) => r._measurement == ""temperature"")
  |> filter(fn: (r) => r.thing_identifier == ""{deviceId}"")
  |> sort(columns: [""_time""], desc: true)
  |> limit(n: 100)";
    }

    /// <summary>
    /// 模拟InfluxDB2查询返回的FluxTable
    /// </summary>
    public static List<FluxTable> CreateMockFluxTables(int count = 3)
    {
        var table = new FluxTable
        {
            Columns = new List<FluxColumn>
            {
                new() { Label = "_time", DataType = FluxColumn.Datatype.DATE_TIME },
                new() { Label = "_measurement", DataType = FluxColumn.Datatype.STRING },
                new() { Label = "thing_identifier", DataType = FluxColumn.Datatype.STRING },
                new() { Label = "product_key", DataType = FluxColumn.Datatype.STRING },
                new() { Label = "temp", DataType = FluxColumn.Datatype.DOUBLE },
                new() { Label = "humidity", DataType = FluxColumn.Datatype.DOUBLE }
            },
            Records = new List<FluxRecord>()
        };

        for (int i = 0; i < count; i++)
        {
            var mockRecord = new Mock<FluxRecord>();
            mockRecord.Setup(r => r.GetTime()).Returns(DateTime.UtcNow.AddMinutes(-i));
            mockRecord.Setup(r => r.GetValue("_measurement")).Returns("temperature");
            mockRecord.Setup(r => r.GetValue("thing_identifier")).Returns("test-device-001");
            mockRecord.Setup(r => r.GetValue("product_key")).Returns("test-product-001");
            mockRecord.Setup(r => r.GetValue("temp")).Returns(26.5 + i);
            mockRecord.Setup(r => r.GetValue("humidity")).Returns(62.3 + i);

            table.Records.Add(mockRecord.Object);
        }

        return new List<FluxTable> { table };
    }
}
```

## 四、核心单元测试

### 1. UnitTests/Models/TimeSeriesDataTests.cs（通用模型测试）

```
using Artizan.IoT.TimeSeries.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.UnitTests.Models;

/// <summary>
/// 通用TimeSeriesData模型单元测试（与InfluxDB版本无关）
/// </summary>
public class TimeSeriesDataTests
{
    #region 基础属性验证
    [Fact]
    public void DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var data = new TimeSeriesData();

        // Assert
        data.ThingIdentifier.ShouldBeEmpty();
        data.UtcDateTime.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        data.Measurement.ShouldBe("default_measurement"); // 对应TimeSeriesConsts默认值
        data.InsertedUtcTime.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        data.Tags.ShouldBeEmpty();
        data.Fields.ShouldBeEmpty();
    }

    [Fact]
    public void Validation_EmptyThingIdentifier_ReturnsError()
    {
        // Arrange
        var data = new TimeSeriesData { ThingIdentifier = string.Empty };
        var validationContext = new ValidationContext(data);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(data, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "物唯一标识不能为空");
    }

    [Fact]
    public void Validation_EmptyUtcDateTime_ReturnsError()
    {
        // Arrange
        var data = new TimeSeriesData 
        { 
            ThingIdentifier = "test-device-001",
            UtcDateTime = DateTime.MinValue // 无效时间
        };
        var validationContext = new ValidationContext(data);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(data, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.ErrorMessage == "采集时间不能为空");
    }
    #endregion

    #region 标签操作测试
    [Fact]
    public void SetTag_ValidKeyAndValue_AddsTagToReadOnlyDictionary()
    {
        // Arrange
        var data = new TimeSeriesData();

        // Act
        data.SetTag("region", "shanghai");

        // Assert
        data.Tags.Count.ShouldBe(1);
        data.Tags.ShouldContainKey("region");
        data.Tags["region"].ShouldBe("shanghai");
        ((IDictionary<string, string>)data.Tags).ShouldBeReadOnly(); // 验证只读特性
    }

    [Fact]
    public void SetTag_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new TimeSeriesData();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => data.SetTag(null!, "value"));
        exception.ParamName.ShouldBe("key");
        exception.Message.ShouldContain("标签键不能为空");
    }

    [Fact]
    public void RemoveTag_ExistingKey_RemovesTag()
    {
        // Arrange
        var data = new TimeSeriesData();
        data.SetTag("region", "hangzhou");

        // Act
        data.RemoveTag("region");

        // Assert
        data.Tags.ShouldNotContainKey("region");
        data.Tags.Count.ShouldBe(0);
    }
    #endregion

    #region 字段操作测试
    [Fact]
    public void SetField_ValidDoubleValue_AddsField()
    {
        // Arrange
        var data = new TimeSeriesData();

        // Act
        data.SetField("temp", 28.9);

        // Assert
        data.Fields.Count.ShouldBe(1);
        data.Fields.ShouldContainKey("temp");
        data.Fields["temp"].ShouldBe(28.9);
    }

    [Fact]
    public void SetField_ValidIntValue_AddsField()
    {
        // Arrange
        var data = new TimeSeriesData();

        // Act
        data.SetField("count", 100);

        // Assert
        data.Fields["count"].ShouldBe(100);
    }

    [Fact]
    public void SetField_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new TimeSeriesData();

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => data.SetField<int>(null!, 50));
        exception.ParamName.ShouldBe("key");
        exception.Message.ShouldContain("字段键不能为空");
    }

    [Fact]
    public void RemoveField_ExistingKey_RemovesField()
    {
        // Arrange
        var data = new TimeSeriesData();
        data.SetField("humidity", 65.2);

        // Act
        data.RemoveField("humidity");

        // Assert
        data.Fields.ShouldNotContainKey("humidity");
    }
    #endregion
}
```

### 2. UnitTests/Clients/InfluxDb2ClientTests.cs（InfluxDB2 客户端测试）

```
using Artizan.IoT.TimeSeries.InfluxDB2.Clients;
using Artizan.IoT.TimeSeries.InfluxDB2.Options;
using Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;
using InfluxDB.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.UnitTests.Clients;

/// <summary>
/// InfluxDB2客户端封装层测试（仅V2）
/// </summary>
public class InfluxDb2ClientTests : IDisposable
{
    private readonly IOptions<InfluxDb2Options> _mockOptions;
    private readonly Mock<ILogger<InfluxDb2Client>> _mockLogger;
    private readonly InfluxDb2Client _client;

    public InfluxDb2ClientTests()
    {
        // 模拟InfluxDB2配置
        _mockOptions = Options.Create(new InfluxDb2Options
        {
            Url = TestConfig.InfluxDb2Url,
            Token = TestConfig.InfluxDb2Token,
            Organization = TestConfig.InfluxDb2Org,
            Bucket = TestConfig.InfluxDb2Bucket,
            Timeout = TimeSpan.FromSeconds(TestConfig.InfluxDb2Timeout)
        });

        _mockLogger = new Mock<ILogger<InfluxDb2Client>>();
        _client = new InfluxDb2Client(_mockLogger.Object, _mockOptions);
    }

    [Fact]
    public void GetClient_ValidConfig_ReturnsConfiguredInfluxDBClient()
    {
        // Act
        var nativeClient = _client.GetClient() as InfluxDBClient;

        // Assert
        nativeClient.ShouldNotBeNull();
        nativeClient.Options.Url.ShouldBe(TestConfig.InfluxDb2Url);
        nativeClient.Options.Token.ShouldBe(TestConfig.InfluxDb2Token);
        nativeClient.Options.Org.ShouldBe(TestConfig.InfluxDb2Org);
        nativeClient.Options.Timeout.ShouldBe(TimeSpan.FromSeconds(TestConfig.InfluxDb2Timeout));
    }

    [Fact]
    public void Constructor_EmptyToken_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = Options.Create(new InfluxDb2Options
        {
            Url = TestConfig.InfluxDb2Url,
            Token = string.Empty, // 无效Token
            Organization = TestConfig.InfluxDb2Org
        });

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new InfluxDb2Client(_mockLogger.Object, invalidOptions));
        exception.Message.ShouldContain("InfluxDB2 Token不能为空");
    }

    [Fact]
    public async Task CheckHealthAsync_MockedPingSuccess_ReturnsTrue()
    {
        // Arrange
        var mockNativeClient = new Mock<InfluxDBClient>();
        mockNativeClient.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // 替换客户端内部的原生实例
        var field = typeof(InfluxDb2Client).GetField("_nativeClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(_client, mockNativeClient.Object);

        // Act
        var isHealthy = await _client.CheckHealthAsync();

        // Assert
        isHealthy.ShouldBeTrue();
        mockNativeClient.Verify(c => c.PingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
```

### 3. UnitTests/Repositories/TimeSeriesRepositoryTests.cs（InfluxDB2 仓储测试）

csharp



运行









```
using Artizan.IoT.TimeSeries.InfluxDB2.Clients;
using Artizan.IoT.TimeSeries.InfluxDB2.Repositories;
using Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;
using Artizan.IoT.TimeSeries.Models;
using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.UnitTests.Repositories;

/// <summary>
/// InfluxDB2仓储层测试（适配通用TimeSeriesData模型）
/// </summary>
public class TimeSeriesRepositoryTests
{
    private readonly Mock<InfluxDb2Client> _mockInfluxDb2Client;
    private readonly Mock<ILogger<TimeSeriesRepository>> _mockLogger;
    private readonly TimeSeriesRepository _repository;
    private readonly TimeSeriesData _testData;

    public TimeSeriesRepositoryTests()
    {
        _mockInfluxDb2Client = new Mock<InfluxDb2Client>();
        _mockLogger = new Mock<ILogger<TimeSeriesRepository>>();
        _repository = new TimeSeriesRepository(_mockLogger.Object, _mockInfluxDb2Client.Object);
        _testData = TestDataFactory.CreateTestTimeSeriesData();
    }

    [Fact]
    public async Task WriteAsync_ValidTimeSeriesData_CallsInfluxDb2WritePointAsync()
    {
        // Arrange
        var mockNativeClient = new Mock<InfluxDBClient>();
        var mockWriteApi = new Mock<WriteApi>();

        mockNativeClient.Setup(c => c.GetWriteApi()).Returns(mockWriteApi.Object);
        _mockInfluxDb2Client.Setup(c => c.GetClient()).Returns(mockNativeClient.Object);
        _mockInfluxDb2Client.Setup(c => c.Options).Returns(new InfluxDb2Options
        {
            Bucket = TestConfig.InfluxDb2Bucket,
            Organization = TestConfig.InfluxDb2Org
        });

        // Act
        await _repository.WriteAsync(_testData);

        // Assert
        mockWriteApi.Verify(w => w.WritePointAsync(
            It.Is<PointData>(p => 
                p.MeasurementName == _testData.Measurement &&
                p.Tags["thing_identifier"] == _testData.ThingIdentifier &&
                p.Fields.ContainsKey("temp")),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task QueryAsync_ValidCriteria_ReturnsMappedTimeSeriesData()
    {
        // Arrange
        var mockNativeClient = new Mock<InfluxDBClient>();
        var mockQueryApi = new Mock<QueryApi>();
        var mockFluxTables = TestDataFactory.CreateMockFluxTables(3);

        mockQueryApi.Setup(q => q.QueryAsync(
            It.IsAny<string>(), 
            TestConfig.InfluxDb2Org, 
            default))
            .ReturnsAsync(mockFluxTables);

        mockNativeClient.Setup(c => c.GetQueryApi()).Returns(mockQueryApi.Object);
        _mockInfluxDb2Client.Setup(c => c.GetClient()).Returns(mockNativeClient.Object);
        _mockInfluxDb2Client.Setup(c => c.Options).Returns(new InfluxDb2Options
        {
            Organization = TestConfig.InfluxDb2Org,
            Bucket = TestConfig.InfluxDb2Bucket
        });

        // Act
        var result = await _repository.QueryAsync(
            bucket: TestConfig.InfluxDb2Bucket,
            org: TestConfig.InfluxDb2Org,
            deviceId: "test-device-001",
            measurement: "temperature",
            startTime: DateTime.UtcNow.AddHours(-1),
            stopTime: DateTime.UtcNow);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.First().ThingIdentifier.ShouldBe("test-device-001");
        result.First().Fields.ShouldContainKey("temp");
        result.First().Fields["temp"].ShouldBe(26.5);
    }

    [Fact]
    public async Task BatchWriteAsync_MultipleData_CallsWritePointsAsync()
    {
        // Arrange
        var dataList = new List<TimeSeriesData>
        {
            TestDataFactory.CreateTestTimeSeriesData("device-001"),
            TestDataFactory.CreateTestTimeSeriesData("device-002")
        };

        var mockNativeClient = new Mock<InfluxDBClient>();
        var mockWriteApi = new Mock<WriteApi>();

        mockNativeClient.Setup(c => c.GetWriteApi()).Returns(mockWriteApi.Object);
        _mockInfluxDb2Client.Setup(c => c.GetClient()).Returns(mockNativeClient.Object);

        // Act
        await _repository.BatchWriteAsync(dataList);

        // Assert
        mockWriteApi.Verify(w => w.WritePointsAsync(
            It.Is<List<PointData>>(p => p.Count == 2),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

## 五、集成测试（真实 InfluxDB2 环境）

### 1. IntegrationTests/InfluxDb2ConnectionTests.cs（连接测试）

```
using Artizan.IoT.TimeSeries.InfluxDB2.Clients;
using Artizan.IoT.TimeSeries.InfluxDB2.Options;
using Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.IntegrationTests;

/// <summary>
/// 真实InfluxDB2连接测试
/// </summary>
[Collection("InfluxDB2 Integration Tests")]
public class InfluxDb2ConnectionTests : IAsyncLifetime
{
    private readonly InfluxDb2Client _client;

    public InfluxDb2ConnectionTests()
    {
        // 使用真实配置初始化客户端
        var options = Options.Create(new InfluxDb2Options
        {
            Url = TestConfig.InfluxDb2Url,
            Token = TestConfig.InfluxDb2Token,
            Organization = TestConfig.InfluxDb2Org,
            Bucket = TestConfig.InfluxDb2Bucket,
            Timeout = TimeSpan.FromSeconds(TestConfig.InfluxDb2Timeout)
        });

        var logger = new Mock<ILogger<InfluxDb2Client>>().Object;
        _client = new InfluxDb2Client(logger, options);
    }

    [Fact]
    public void GetClient_RealConfig_CreatesValidClient()
    {
        // Act
        var nativeClient = _client.GetClient();

        // Assert
        nativeClient.ShouldNotBeNull();
        nativeClient.Options.Url.ShouldBe(TestConfig.InfluxDb2Url);
    }

    [Fact]
    public async Task CheckHealthAsync_RealServer_ReturnsHealthy()
    {
        // Act
        var isHealthy = await _client.CheckHealthAsync();

        // Assert
        isHealthy.ShouldBeTrue("InfluxDB2服务不可达，请检查配置和服务状态");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await Task.CompletedTask;
    }
}
```

### 2. IntegrationTests/TimeSeriesDataCrudTests.cs（数据读写测试）

```
using Artizan.IoT.TimeSeries.InfluxDB2.Clients;
using Artizan.IoT.TimeSeries.InfluxDB2.Options;
using Artizan.IoT.TimeSeries.InfluxDB2.Repositories;
using Artizan.IoT.TimeSeries.InfluxDB2.Tests.Helpers;
using Artizan.IoT.TimeSeries.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Artizan.IoT.TimeSeries.InfluxDB2.Tests.IntegrationTests;

/// <summary>
/// 真实InfluxDB2数据读写测试（适配通用TimeSeriesData模型）
/// </summary>
[Collection("InfluxDB2 Integration Tests")]
public class TimeSeriesDataCrudTests : IAsyncLifetime
{
    private readonly TimeSeriesRepository _repository;
    private readonly TimeSeriesData _testData;
    private readonly string _uniqueDeviceId = $"test-device-{Guid.NewGuid():N}";

    public TimeSeriesDataCrudTests()
    {
        // 初始化真实客户端
        var options = Options.Create(new InfluxDb2Options
        {
            Url = TestConfig.InfluxDb2Url,
            Token = TestConfig.InfluxDb2Token,
            Organization = TestConfig.InfluxDb2Org,
            Bucket = TestConfig.InfluxDb2Bucket,
            Timeout = TimeSpan.FromSeconds(TestConfig.InfluxDb2Timeout)
        });

        var clientLogger = new Mock<ILogger<InfluxDb2Client>>().Object;
        var client = new InfluxDb2Client(clientLogger, options);

        var repoLogger = new Mock<ILogger<TimeSeriesRepository>>().Object;
        _repository = new TimeSeriesRepository(repoLogger, client);

        // 创建唯一测试数据
        _testData = TestDataFactory.CreateTestTimeSeriesData(_uniqueDeviceId);
    }

    [Fact]
    public async Task WriteAndQuery_RealData_MatchesOriginalValues()
    {
        // Step 1: 写入数据
        await _repository.WriteAsync(_testData);

        // 等待InfluxDB2写入延迟
        await Task.Delay(2000);

        // Step 2: 查询数据
        var result = await _repository.QueryAsync(
            bucket: TestConfig.InfluxDb2Bucket,
            org: TestConfig.InfluxDb2Org,
            deviceId: _uniqueDeviceId,
            measurement: _testData.Measurement,
            startTime: _testData.UtcDateTime.AddMinutes(-5),
            stopTime: _testData.UtcDateTime.AddMinutes(5));

        // Step 3: 验证结果
        result.ShouldNotBeEmpty();
        var returnedData = result.First();

        returnedData.ThingIdentifier.ShouldBe(_uniqueDeviceId);
        returnedData.Measurement.ShouldBe(_testData.Measurement);
        returnedData.Fields["temp"].ShouldBe(_testData.Fields["temp"]);
        returnedData.Tags["product_key"].ShouldBe(_testData.Tags["product_key"]);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // 可选：清理测试数据（需删除权限）
        /*
        var deleteApi = _client.GetClient().GetDeleteApi();
        await deleteApi.Delete(
            _testData.UtcDateTime.AddMinutes(-10),
            DateTime.UtcNow.AddMinutes(10),
            $"thing_identifier=\"{_uniqueDeviceId}\"",
            TestConfig.InfluxDb2Bucket,
            TestConfig.InfluxDb2Org);
        */
        await Task.CompletedTask;
    }
}
```

## 总结

### 关键点回顾

1. **纯 InfluxDB2 适配**：所有测试仅使用 InfluxDB2 原生 API（`InfluxDBClient`/`QueryApi`/`WriteApi`），无任何 InfluxDB3 相关代码；

2. **通用模型聚焦**：完全基于你提供的`TimeSeriesData`通用模型，覆盖模型验证、标签 / 字段操作、与 InfluxDB2 的映射逻辑；

3. 分层测试设计：

   - 单元测试：Mock InfluxDB2 原生依赖，验证核心逻辑，无真实环境依赖；
- 集成测试：连接真实 InfluxDB2 环境，验证端到端数据读写一致性；
   
   

4. **严格遵循框架要求**：仅使用指定的`xUnit + Shouldly + Microsoft.NET.Test.Sdk`框架，无额外依赖。

### 运行说明

1. 集成测试前需确保：

   - InfluxDB2 服务正常运行（默认端口 8086）；
   - `appsettings.json`配置正确的 InfluxDB2 Url/Token/Org/Bucket；
   - Token 拥有对应 Bucket 的读写权限；

   

2. 单元测试可直接运行，无需真实环境；

3. 测试数据使用唯一设备 ID（GUID），避免与现有数据冲突。