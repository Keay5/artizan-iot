using Artizan.IoT;
using Artizan.IoT.ThingModels.Tsls;
using Artizan.IoT.ThingModels.Tsls.DataObjects;
using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;
using Artizan.IoT.ThingModels.Tsls.Exceptions;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.Serializations;
using Artizan.IoTHub.Products.Dtos;
using Artizan.IoTHub.Products.Modules.Dtos;
using Artizan.IoTHub.Products.Properties;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products.Modules;

public abstract class ProductModuleAppService_Tests<TStartupModule> : IoTHubApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProductAppService _productAppService;
    private readonly IProductModuleAppService _productModuleAppService;
    private readonly IProductModuleRepository _productModuleRepository;
    private readonly IGuidGenerator _guidGenerator;

    protected ProductModuleAppService_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _productModuleAppService = GetRequiredService<IProductModuleAppService>();
        _productModuleRepository = GetRequiredService<IProductModuleRepository>();
        _guidGenerator = GetRequiredService<IGuidGenerator>();
    }

    #region Moudle

    [Fact]
    public async Task CreateProductModuleAsync()
    {
        var product = await CreateProductForTestAsync();

        var createProductModuleInput = new CreateProductModuleInput
        {
            ProductId = product.Id,
            Name = "智能感应",
            Identifier = "SmartSensing",
        };

        var productModule = await _productModuleAppService.CreateProductModuleAsync(createProductModuleInput);
        productModule.ShouldNotBeNull();
        productModule.Name.ShouldNotBeNull();
        productModule.Identifier.ShouldNotBeNull();
        productModule.ProductModuleTsl.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateProductModuleAsync()
    {
        var product = await CreateProductForTestAsync();

        var name = "智能感应";
        var identifier = "SmartSensing";
        string? desc = null;
        var createProductModuleInput = new CreateProductModuleInput
        {
            ProductId = product.Id,
            Name = name,
            Identifier = identifier,
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleAsync(createProductModuleInput);
        productModuleDto.ShouldNotBeNull();
        productModuleDto.Name.ShouldBe(name);
        productModuleDto.Identifier.ShouldBe(identifier);
        productModuleDto.ProductModuleTsl.ShouldNotBeNull();
        productModuleDto.Description.ShouldBeNull();

        name = "风扇控制模块";
        identifier = "FanControl";
        desc = "描述：风扇控制模块";
        var upateInput = new UpdateProductModuleInput
        {
            Name = name,
            Identifier = identifier,
            Description = desc
        };
        productModuleDto = await _productModuleAppService.UpdateProductModuleAsync(productModuleDto.Id, upateInput);

        productModuleDto.ShouldNotBeNull();
        productModuleDto.Name.ShouldBe(name);
        productModuleDto.Identifier.ShouldBe(identifier);
        productModuleDto.Description.ShouldBe(desc);
    }

    [Fact]
    public async Task DeleteProductModuleAsync()
    {
        var product = await CreateProductForTestAsync();

        var name = "智能感应";
        var identifier = "SmartSensing";
        string? desc = null;
        var createProductModuleInput = new CreateProductModuleInput
        {
            ProductId = product.Id,
            Name = name,
            Identifier = identifier,
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleAsync(createProductModuleInput);
        productModuleDto.ShouldNotBeNull();
        productModuleDto.Name.ShouldBe(name);
        productModuleDto.Identifier.ShouldBe(identifier);
        productModuleDto.ProductModuleTsl.ShouldNotBeNull();
        productModuleDto.Description.ShouldBeNull();

        await _productModuleAppService.DeleteProductModuleAsync(productModuleDto.Id);
        var productModule = await _productModuleRepository.FindAsync(productModuleDto.Id);
        productModule.ShouldBeNull();
    }

    #endregion

    #region Module Property

    [Fact]
    public async Task CreateProductModuleProperty_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();

        productModule.ProductModuleTsl.ShouldNotBeNull();

        int totalCount = 0;

        //--------------------------------Property：Int32
        var identifier = "windSpeed";
        var createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "风速档位",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            DataType = DataTypes.Int32,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "1", Max = "5", Step = "1", Unit = "gk", UnitName = "档" }),
            Description = "风速档位（1 = 最小，5 = 最大）"
        };

        var productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

        //--------------------------------Property：Float
        identifier = "ambientTemperature";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "环境温度",
            Identifier = identifier,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Float,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "-10", Max = "60", Step = "0.5" }),
            Description = "环境温度（精度 ±0.5°C）",
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);


        //--------------------------------Property：Double
        identifier = "ambientHumidity";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "环境湿度",
            Identifier = identifier,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Float,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "-10.01", Max = "60.99", Step = "0.01" }),
            Description = "环境湿度（精度 ±0.01°C）",
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

        //--------------------------------Property：Bool
        identifier = "powerStatus";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "风扇开关状态",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            Required = true,
            DataType = DataTypes.Boolean,
            SpecsDoJsonString = TslSerializer.SerializeObject(new KeyValueSpecsDo { Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" } } }),
            Description = "风扇开关状态",
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);


        //--------------------------------Property：Enum
        identifier = "windMode";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "风类模式",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            Required = true,
            DataType = DataTypes.Enum,
            SpecsDoJsonString = TslSerializer.SerializeObject(new KeyValueSpecsDo
            {
                Values = new Dictionary<string, string> { { "0", "正常" }, { "1", "自然" }, { "2", "睡眠" }, { "3", "强力" } }
            }),
            Description = "风类模式（正常 / 自然 / 睡眠 /turbo 强力）"
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

        //--------------------------------Property：Text
        identifier = "SerialNumber";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "序列号",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            Required = false,
            DataType = DataTypes.Text,
            SpecsDoJsonString = TslSerializer.SerializeObject(new StringSpecsDo
            {
                Length = "1000"
            }),
            Description = "序列号（唯一标识个体的编号，设备 SN 是平台识别单台设备的唯一凭证）"
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

        //--------------------------------Property：Date
        identifier = "localDate";
        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "当地日期",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            Required = false,
            DataType = DataTypes.Date,
            SpecsDoJsonString = TslSerializer.SerializeObject(new EmptySpecsDo
            {
            }),
            Description = "当地日期"
        };

        productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

    }

    [Fact]
    public async Task CreateProductModuleProperty_Struct_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();

        productModule.ProductModuleTsl.ShouldNotBeNull();

        int totalCount = 0;

        //--------------------------------Property：Struct
        var identifier = "dailyConsumption";
        //var specsDojson = "{\"fields\":[{\"identifier\":\"date\",\"name\":\"日期\",\"dataType\":\"date\",\"specsDo\":{}},{\"identifier\":\"consumption\",\"name\":\"耗电量\",\"dataType\":\"float\",\"specsDo\":{\"min\":\"0\",\"max\":\"12333456\",\"step\":\"0.1\",\"unit\":\"Wh\",\"unitName\":\"瓦时\"}},{\"identifier\":\"lat\",\"name\":\"纬度\",\"dataType\":\"double\",\"specsDo\":{\"min\":\"-90\",\"max\":\"90\"}},{\"identifier\":\"lng\",\"name\":\"经度\",\"dataType\":\"double\",\"specsDo\":{\"min\":\"-180\",\"max\":\"180\"}}]}";
        //var specsDojson = "{\"min\":\"1\",\"max\":\"5\",\"step\":\"1\",\"unit\":\"gk\",\"unitName\":\"档\"}";

        var createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "当日耗电量",
            Identifier = identifier,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Struct,
            Description = "当日耗电量",
            //SpecsDoJsonString = specsDojson,

            SpecsDoJsonString = TslSerializer.SerializeObject(new StructSpecsDo()
            {
                    // 日期
                new StructFieldDo
                {
                    Identifier = "date",
                        Name="日期",
                        DataType= DataTypes.Date,
                        SpecsDo = new EmptySpecsDo { },
                },
                // 耗电量
                new StructFieldDo{
                    Identifier = "consumption",
                    Name="耗电量",
                    DataType= DataTypes.Float,
                    SpecsDo = new NumericSpecsDo {Min ="0", Max="12333456", Step="0.1", Unit="Wh", UnitName="瓦时"}
                },
                //经度
                new StructFieldDo {
                    Identifier = "lat",
                    Name = "纬度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-90",
                        Max = "90"
                    }
                },
                // 经度
                new StructFieldDo
                {
                    Identifier = "lng",
                    Name = "经度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-180",
                        Max = "180"
                    }
                },
        }),
        };

        var productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);
        var tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);
    }

    [Fact]
    public async Task UpdateProductModulePropertyAsync()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();

        productModule.ProductModuleTsl.ShouldNotBeNull();

        //初始为：Property：Struct
        var identifierOld = "dailyConsumption";
        var createInput = new CreateProductModulePropertyInput()
        {
            Name = "当日耗电量",
            Identifier = identifierOld,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Struct,
            Description = "当日耗电量",
            SpecsDoJsonString = TslSerializer.SerializeObject(new StructSpecsDo()
            {
                    // 日期
                new StructFieldDo
                {
                    Identifier = "date",
                        Name="日期",
                        DataType= DataTypes.Date,
                        SpecsDo = new EmptySpecsDo { },
                },
                // 耗电量
                new StructFieldDo{
                    Identifier = "consumption",
                    Name="耗电量",
                    DataType= DataTypes.Float,
                    SpecsDo = new NumericSpecsDo {Min ="0", Max="12333456", Step="0.1", Unit="Wh", UnitName="瓦时"}
                },
                //经度
                new StructFieldDo {
                    Identifier = "lat",
                    Name = "纬度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-90",
                        Max = "90"
                    }
                },
                // 经度
                new StructFieldDo
                {
                    Identifier = "lng",
                    Name = "经度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-180",
                        Max = "180"
                    }
                },
        }),
        };

        var productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createInput);
        var tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.FirstOrDefault(p => p.Identifier == identifierOld).ShouldNotBeNull();


        //更新为：Property：Float
        var identifier = "ambientTemperature";
        var updateInput = new UpdateProductModulePropertyInput()
        {
            Name = "环境温度",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            Required = false,
            DataType = DataTypes.Float,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "-10", Max = "60", Step = "0.5" }),
            Description = "环境温度（精度 ±0.5°C）",
        };

        productModuleDto = await _productModuleAppService.UpdateProductModulePropertyAsync(productModule.Id, identifierOld,  updateInput);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.FirstOrDefault(p => p.Identifier == identifierOld).ShouldBeNull();
        tsl.Properties.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteProductModulePropertyAsync()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();

        productModule.ProductModuleTsl.ShouldNotBeNull();

        //Property
        var identifier = "dailyConsumption";
        var createInput = new CreateProductModulePropertyInput()
        {
            Name = "当日耗电量",
            Identifier = identifier,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Struct,
            Description = "当日耗电量",
            SpecsDoJsonString = TslSerializer.SerializeObject(new StructSpecsDo()
            {
                    // 日期
                new StructFieldDo
                {
                    Identifier = "date",
                        Name="日期",
                        DataType= DataTypes.Date,
                        SpecsDo = new EmptySpecsDo { },
                },
                // 耗电量
                new StructFieldDo{
                    Identifier = "consumption",
                    Name="耗电量",
                    DataType= DataTypes.Float,
                    SpecsDo = new NumericSpecsDo {Min ="0", Max="12333456", Step="0.1", Unit="Wh", UnitName="瓦时"}
                },
                //经度
                new StructFieldDo {
                    Identifier = "lat",
                    Name = "纬度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-90",
                        Max = "90"
                    }
                },
                // 经度
                new StructFieldDo
                {
                    Identifier = "lng",
                    Name = "经度",
                    DataType = DataTypes.Double,
                    SpecsDo = new NumericSpecsDo
                    {
                        Min = "-180",
                        Max = "180"
                    }
                },
        }),
        };

        var productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createInput);
        var tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();

        productModuleDto = await _productModuleAppService.DeleteProductModulePropertyAsync(productModule.Id, identifier);
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.FirstOrDefault(p => p.Identifier == identifier).ShouldBeNull();
    }

    [Fact]
    public async Task CreateProductModuleProperty_Identifiers_MustBe_Unique_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();

        productModule.ProductModuleTsl.ShouldNotBeNull();

        int totalCount = 0;

        var identifier = "windSpeed";

        var createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "风速档位",
            Identifier = identifier,
            AccessMode = AccessModes.ReadAndWrite,
            DataType = DataTypes.Int32,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "1", Max = "5", Step = "1", Unit = "gk", UnitName = "档" }),
            Description = "风速档位（1 = 最小，5 = 最大）"
        };

        var productModuleDto = await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput);

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Properties.ShouldNotBeNull();
        tsl.Properties.Count.ShouldBe(++totalCount);
        tsl.Properties[totalCount - 1].Identifier.ShouldBe(identifier);

        createProductModuleInput = new CreateProductModulePropertyInput()
        {
            Name = "环境温度",
            Identifier = identifier,
            AccessMode = AccessModes.ReadOnly,
            Required = false,
            DataType = DataTypes.Float,
            SpecsDoJsonString = TslSerializer.SerializeObject(new NumericSpecsDo { Min = "-10", Max = "60", Step = "0.5" }),
            Description = "环境温度（精度 ±0.5°C）",
        };

        // Act & Assert（Shouldly异步断言）
        var exception = await Should.ThrowAsync<TslFormatErrorExeption>(async () =>
            await _productModuleAppService.CreateProductModulePropertyAsync(productModule.Id, createProductModuleInput)
        );
        exception.Code.ShouldBe(IoTAbstractionsErrorCodes.Tsls.TSLFormatError);
        // 可选：断言异常消息（进一步验证）
        //exception.Message.ShouldBe("创建产品模块属性失败：Identifier必须唯一");
        // 或模糊匹配消息
        // exception.Message.ShouldContain("TSL格式有误。具体原因如下：属性标识重复");

    }

    #endregion

    #region Module Service

    [Fact]

    public async Task CreateProductModuleServer_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();
        Tsl Tsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl); ;

        int totalCount = Tsl.Events.Count;
        var identifier = "setChildLock";

        var createInput = new CreateProductModuleServiceInput()
        {
            Identifier = identifier,
            Name = "控制童锁",
            CallType = ServiceCallTypes.Async,
            Required = false,
            InputDatas = [
                new CommonInputParamDo {
                    Identifier = "lockerStatus",
                    Name = "锁的状态",
                    Required = false,
                    DataType = DataTypes.Boolean,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new KeyValueSpecsDo {
                            Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" } }
                    }),
                },
            ],
            Description = "自定义服务, 用户开启/关闭童锁",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleServiceAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Services.ShouldNotBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateProductModuleServer_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();
        Tsl Tsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl); ;

        int totalCount = Tsl.Events.Count;
        var identifier = "setChildLock";

        // 创建
        var createInput = new CreateProductModuleServiceInput()
        {
            Identifier = identifier,
            Name = "控制童锁",
            CallType = ServiceCallTypes.Async,
            Required = false,
            InputDatas = [
        new CommonInputParamDo {
                    Identifier = "lockerStatus",
                    Name = "锁的状态（Boolean）",
                    Required = false,
                    DataType = DataTypes.Boolean,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new KeyValueSpecsDo {
                            Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" } }
                    }),
                },
            ],
            Description = "自定义服务, 用户开启/关闭童锁",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleServiceAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();
        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Services.ShouldNotBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();

        // 更新
        var newIdentifier = "autoSmartLockerStatus";
        var updateInput = new UpdateProductModuleServiceInput()
        {
            Identifier = newIdentifier,
            Name = "智能控制童锁",
            CallType = ServiceCallTypes.Async,
            Required = false,
            InputDatas = [
                new CommonInputParamDo {
                    Identifier = "SmartLockerStatus",
                    Name = "智慧锁的状态（枚举）",
                    Required = false,
                    DataType = DataTypes.Enum,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new KeyValueSpecsDo {
                            Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" }, { "2", "自动" } }
                    }),
                },
            ],
            Description = "自定义服务, 用户开启/关闭童锁（智能版）",
        };

        productModuleDto = await _productModuleAppService.UpdateProductModuleServiceAsync(productModule.Id, identifier, updateInput);
        productModuleDto.ShouldNotBeNull();
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Services.ShouldNotBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == identifier).ShouldBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == newIdentifier).ShouldNotBeNull();
    }


    [Fact]
    public async Task DeleteProductModuleServer_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();
        Tsl Tsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl); ;

        int totalCount = Tsl.Events.Count;
        var identifier = "setChildLock";

        // 添加
        var createInput = new CreateProductModuleServiceInput()
        {
            Identifier = identifier,
            Name = "控制童锁",
            CallType = ServiceCallTypes.Async,
            Required = false,
            InputDatas = [
                new CommonInputParamDo {
                    Identifier = "lockerStatus",
                    Name = "锁的状态",
                    Required = false,
                    DataType = DataTypes.Boolean,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new KeyValueSpecsDo {
                            Values = new Dictionary<string, string> { { "0", "关闭" }, { "1", "开启" } }
                    }),
                },
            ],
            Description = "自定义服务, 用户开启/关闭童锁",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleServiceAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Services.ShouldNotBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();

        // 删除
        productModuleDto = await _productModuleAppService.DeleteProductModuleServiceAsync(productModule.Id, identifier);
        productModuleDto.ShouldNotBeNull();
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Services.ShouldNotBeNull();
        tsl.Services.FirstOrDefault(p => p.Identifier == identifier).ShouldBeNull();
    }


    #endregion

    #region Module Event

    [Fact]
    public async Task CreateProductModuleEvent_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();
        Tsl Tsl = TslSerializer.DeserializeObject<Tsl>(productModule.ProductModuleTsl); ;

        int totalCount = Tsl.Events.Count;
        var identifier = "LowBatteyEvent";

        var createInput = new CreateProductModuleEventInput()
        {
            Identifier = identifier,
            Name = "电量低告警",
            EventType = EventTypes.Alert,
            Required = false,
            OutputDatas = [
                new OutputParamDo {
                    Identifier = "BatteryLevel",
                    Name = "电量水平",
                    DataType = DataTypes.Int32,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new NumericSpecsDo {
                            Min = "1",
                            Max = "100",
                            Step = "1",
                        }
                    ),
                },
            ],
            Description = "风风扇电池电量低时告警",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };

        var productModuleDto = await _productModuleAppService.CreateProductModuleEventAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();

        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.Count.ShouldBe(++totalCount);
        tsl.Events[totalCount - 1].Identifier.ShouldBe(identifier);
    }

    [Fact]
    public async Task UpdateProductModuleEvent_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();

        // 添加
        var identifier = "LowBatteyEvent";
        var createInput = new CreateProductModuleEventInput()
        {
            Identifier = identifier,
            Name = "电量低告警",
            EventType = EventTypes.Alert,
            Required = false,
            OutputDatas = [
                new OutputParamDo {
                    Identifier = "BatteryLevel",
                    Name = "电量水平",
                    DataType = DataTypes.Int32,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new NumericSpecsDo {
                            Min = "1",
                            Max = "100",
                            Step = "1",
                        }
                    ),
                },
            ],
            Description = "风风扇电池电量低时告警",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };
        var productModuleDto = await _productModuleAppService.CreateProductModuleEventAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();
        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();


        // 更新
        var newIdentifier = "LowBatteyEvent_v2";
        var updateInput = new UpdateProductModuleEventInput()
        {
            Identifier = newIdentifier,
            Name = "电量低告警(v2)",
            EventType = EventTypes.Alert,
            Required = false,
            OutputDatas = [
                new OutputParamDo {
                    Identifier = "BatteryLevel_v2",
                    Name = "电量水平",
                    DataType = DataTypes.Int32,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new NumericSpecsDo {
                            Min = "1",
                            Max = "100",
                            Step = "1",
                        }
                    ),
                },
            ],
            Description = "风风扇电池电量低时告警v2",
        };
        productModuleDto = await _productModuleAppService.UpdateProductModuleEventAsync(productModule.Id, identifier, updateInput);
        productModuleDto.ShouldNotBeNull();
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.FirstOrDefault(p => p.Identifier == identifier).ShouldBeNull();
        tsl.Events.FirstOrDefault(p => p.Identifier == newIdentifier).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteProductModuleEvent_Async()
    {
        // 创建Product、ProductModule
        var (product, productModule) = await CreateProductModuleForTestAsync();
        productModule.ProductModuleTsl.ShouldNotBeNull();

        // 添加
        var identifier = "LowBatteyEvent";
        var createInput = new CreateProductModuleEventInput()
        {
            Identifier = identifier,
            Name = "电量低告警",
            EventType = EventTypes.Alert,
            Required = false,
            OutputDatas = [
                new OutputParamDo {
                    Identifier = "BatteryLevel",
                    Name = "电量水平",
                    DataType = DataTypes.Int32,
                    SpecsDoJsonString = TslSerializer.SerializeObject(
                        new NumericSpecsDo {
                            Min = "1",
                            Max = "100",
                            Step = "1",
                        }
                    ),
                },
            ],
            Description = "风风扇电池电量低时告警",
            ConcurrencyStamp = productModule.ConcurrencyStamp
        };
        var productModuleDto = await _productModuleAppService.CreateProductModuleEventAsync(productModule.Id, createInput);
        productModuleDto.ShouldNotBeNull();
        Tsl tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.FirstOrDefault(p => p.Identifier == identifier).ShouldNotBeNull();


        // 删除
        productModuleDto = await _productModuleAppService.DeleteProductModuleEventAsync(productModule.Id, identifier);
        productModuleDto.ShouldNotBeNull();
        tsl = TslSerializer.DeserializeObject<Tsl>(productModuleDto.ProductModuleTsl);
        tsl.ShouldNotBeNull();
        tsl.Events.ShouldNotBeNull();
        tsl.Events.FirstOrDefault(p => p.Identifier == identifier).ShouldBeNull();
    }

    #endregion

    #region 辅助方法
    protected async Task<ProductDto> CreateProductForTestAsync()
    {
        //-------------------创建产品-----------------------
        var createProductInput = new CreateProductInput
        {
            ProductName = "智能SmartFan_VX100_01",
            Category = ProductCategory.CustomCategory,
            CategoryName = "智能风扇VX100系列",
            NodeType = ProductNodeTypes.DirectConnectionEquipment,
            NetworkingMode = ProductNetworkingModes.WiFi,
            AccessGatewayProtocol = null,
            DataFormat = ProductDataFormat.ICAStandardDataFormat,
            AuthenticationMode = ProductAuthenticationMode.DeviceSecret,
            IsEnableDynamicRegistration = true,
            IsUsingPrivateCACertificate = false,
            Description = "智能风扇系列 型号VX100#01"
        };

        var product = await _productAppService.CreateAsync(createProductInput);

        product.ShouldNotBeNull();
        product.ProductKey.ShouldNotBeNull();

        return product;
    }

    public async Task<(ProductDto, ProductModuleDto)> CreateProductModuleForTestAsync()
    {
        var product = await CreateProductForTestAsync();

        var createProductModuleInput = new CreateProductModuleInput
        {
            ProductId = product.Id,
            Identifier = "SmartSensing",
            Name = "智能感应",
        };

        var productModule = await _productModuleAppService.CreateProductModuleAsync(createProductModuleInput);
        productModule.ShouldNotBeNull();
        productModule.Name.ShouldNotBeNull();
        productModule.Identifier.ShouldNotBeNull();
        productModule.ProductModuleTsl.ShouldNotBeNull();

        return (product, productModule);
    }

    #endregion
}
