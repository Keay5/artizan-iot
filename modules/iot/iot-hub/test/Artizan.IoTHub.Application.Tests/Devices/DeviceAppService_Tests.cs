using Artizan.IoT.ThingModels.Tsls.Exceptions;
using Artizan.IoTHub;
using Artizan.IoTHub.Devices;
using Artizan.IoTHub.Devices.Dtos;
using Artizan.IoTHub.Devices.Exceptions;
using Artizan.IoTHub.Products;
using Artizan.IoTHub.Products.Dtos;
using Artizan.IoTHub.Products.Modules;
using Artizan.IoTHub.Products.Modules.Dtos;
using Artizan.IoTHub.Products.Properties;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Xunit;

namespace MsOnAbp.IoTHub.Devices;

public abstract class DeviceAppService_Tests<TStartupModule> : IoTHubApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IDeviceAppService _deviceAppService;
    private readonly IProductAppService _productAppService;
    private readonly IProductLookupAppService _productLookupAppService;
    private readonly IDeviceRepository _deviceRepository;

    protected DeviceAppService_Tests()
    {
        _deviceAppService = GetRequiredService<IDeviceAppService>();
        _productAppService = GetRequiredService<IProductAppService>();
        _productLookupAppService = GetRequiredService<IProductLookupAppService>();
        _deviceRepository = GetRequiredService<IDeviceRepository>();
    }

    #region 辅助方法

    protected async Task<ProductDto> CreateProductAsync()
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
   
    protected async Task<DeviceDto> CreateDeviceAsync(ProductDto product, string deviceName = null)
    {
        deviceName = deviceName ?? "TestDeviceName";    

        var createDeviceInput = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName,
            RemarkName = $"测试设备_{deviceName}"
        };

        return await _deviceAppService.CreateAsync(createDeviceInput);
    } 
    #endregion

    [Fact]
    public async Task CreateDeviceTestAsync()
    {
        var targetProduct = await CreateProductAsync();

        //-------------------Lookup产品-----------------------
        var products = await _productLookupAppService.GetListAsync();
        var product = products.Items.SingleOrDefault(p => p.Id == targetProduct.Id);
        product.ShouldNotBeNull();

        //-------------------创建设备-----------------------
        var deviceName = "Device_01_SmartFan_VX100_01";
        var createDeviceInput = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName,
            RemarkName = "设备01智能风扇VX100"
        };
        var device = await _deviceAppService.CreateAsync(createDeviceInput);

        device.ShouldNotBeNull();
        device.ProductId.ShouldBe(product.Id);
        device.DeviceName.ShouldBe(deviceName);
    }

    [Fact]
    public async Task CreatDevice_NameInvalidTestAsync()
    {
        var product = await CreateProductAsync();

        // 设备名称长度为 4~32 个字符，可以包含英文字母、数字和特殊字符：短划线（-）、下划线（_）、at（@）、半角句号（.）、半角冒号（:）。
        var deviceName1 = "A-za_Z@P.T:Dev2.0";
        var createDeviceInput1 = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName1,
            RemarkName = null
        };
        var exception1 = await Record.ExceptionAsync(async () =>
             await _deviceAppService.CreateAsync(createDeviceInput1));
        // 验证异常类型
        Assert.Null(exception1);

        // Arrange
        var deviceName2 = "Device_01@$SmartFan_VX100_01"; //不支持 '$' 符号
        var createDeviceInput2 = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName2,
            RemarkName = null
        };

        var exception2 = await Record.ExceptionAsync(async () =>
            await _deviceAppService.CreateAsync(createDeviceInput2));

        // Assert
        // 验证异常类型
        Assert.NotNull(exception2);
        var businessEx = Assert.IsType<BusinessException>(exception2);

        // 验证异常属性（如消息、错误码）
        Assert.Equal(IoTHubErrorCodes.DeviceNameInvalid, businessEx.Code);
    }

    [Fact]
    public async Task CreateDevice_DuplicateNamenTestAsync()
    {
        var product = await CreateProductAsync();

        // Arrange
        var deviceName = "Device_01@SmartFan_VX100_01"; 
        var createDeviceInput1 = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName,
            RemarkName = "设备01智能风扇VX100"
        };
        var createDeviceInput2 = new CreateDeviceInput
        {
            ProductId = product.Id,
            DeviceName = deviceName,
            RemarkName = "设备02智能风扇VX100"
        };
        await _deviceAppService.CreateAsync(createDeviceInput1);

        var exception = await Record.ExceptionAsync(async () =>
            await _deviceAppService.CreateAsync(createDeviceInput2));

        // Assert
        // 验证异常类型
        Assert.NotNull(exception);
        var businessEx = Assert.IsType<DuplicateDeviceNameException>(exception);

        // 验证异常属性（如消息、错误码）
        Assert.Equal(IoTHubErrorCodes.DuplicateDeviceName, businessEx.Code);

    }

    [Fact]
    public async Task UpdateDeviceTestAsync()
    {
        // Arrange
        var product = await CreateProductAsync();
        var device = await CreateDeviceAsync(product, deviceName: "UpdateDeviceTest");

        var updateInput = new UpdateDeviceInput
        {
            DeviceName = $"{device.DeviceName}_Updated",
            RemarkName = "更新后的测试设备",
            Description = "这是更新后的设备描述信息",
            ConcurrencyStamp = device.ConcurrencyStamp
        };

        // Act
        var updatedDeviceDto = await _deviceAppService.UpdateAsync(device.Id, updateInput);

        // Assert
        updatedDeviceDto.ShouldNotBeNull();
        updatedDeviceDto.Id.ShouldBe(device.Id);
        updatedDeviceDto.DeviceName.ShouldBe(updateInput.DeviceName);
        updatedDeviceDto.RemarkName.ShouldBe(updateInput.RemarkName);
        updatedDeviceDto.Description.ShouldBe(updateInput.Description);

        // 验证实际数据已更新
    }

    [Fact]
    public async Task DeleteDeviceAsync()
    {
        // Arrange
        var product = await CreateProductAsync();
        var device = await CreateDeviceAsync(product);

        // Act
        await _deviceAppService.DeleteAsync(device.Id);

        // Act & Assert（Shouldly异步断言）验证设备已被删除
        var exception = await Should.ThrowAsync<EntityNotFoundException>(async () =>
             await _deviceRepository.GetAsync(device.Id)
        );

        exception.ShouldNotBeNull();
    }
}
