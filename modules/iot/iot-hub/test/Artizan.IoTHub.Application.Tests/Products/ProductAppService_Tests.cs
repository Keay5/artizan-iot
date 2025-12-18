using Artizan.IoTHub.Products.Dtos;
using Artizan.IoTHub.Products.Properties;
using Shouldly;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products;

public abstract class ProductAppService_Tests<TStartupModule> : IoTHubApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProductAppService _productAppService;
    protected IProductKeyGenerator ProductKeyGenerator { get; }

    protected ProductAppService_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
        ProductKeyGenerator = GetRequiredService<IProductKeyGenerator>();
    }

    //[Fact]
    //public async Task GetAsync()
    //{
    //    var result = await _productAppService.GetAsync();
    //    result.Value.ShouldBe(42);
    //}

    //[Fact]
    //public async Task GetAuthorizedAsync()
    //{
    //    var result = await _productAppService.GetAuthorizedAsync();
    //    result.Value.ShouldBe(42);
    //}

    [Fact]
    public async Task CreateProductAsync()
    {
        var input = new CreateProductInput
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

        var result = await _productAppService.CreateAsync(input);


        result.ShouldNotBeNull();
        result.ProductKey.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateProduct_NameInvalidAsync()
    {
        // Arrange
        var input1 = new CreateProductInput
        {
            //产品名称长度为 4~30 个字符，可以包含中文、英文字母、数字和下划线（_）。一个中文算 2 个字符。
            ProductName = "智能SmartFan_VX100",  
            Category = ProductCategory.CustomCategory,
            CategoryName = "智能风扇VX系列",
            NodeType = ProductNodeTypes.DirectConnectionEquipment,
            NetworkingMode = ProductNetworkingModes.WiFi,
            AccessGatewayProtocol = null,
            DataFormat = ProductDataFormat.ICAStandardDataFormat,
            AuthenticationMode = ProductAuthenticationMode.DeviceSecret,
            IsEnableDynamicRegistration = true,
            IsUsingPrivateCACertificate = false,
            Description = "智能风扇系列 型号VX100"
        };

        var exception1 = await Record.ExceptionAsync(async () =>
            await _productAppService.CreateAsync(input1));

        Assert.Null(exception1);

        // Arrange
        var input2 = new CreateProductInput
        {
            ProductName = "智能SmartFan-VX100",  // 不支持 '-'
            Category = ProductCategory.CustomCategory,
            CategoryName = "智能风扇VX系列",
            NodeType = ProductNodeTypes.DirectConnectionEquipment,
            NetworkingMode = ProductNetworkingModes.WiFi,
            AccessGatewayProtocol = null,
            DataFormat = ProductDataFormat.ICAStandardDataFormat,
            AuthenticationMode = ProductAuthenticationMode.DeviceSecret,
            IsEnableDynamicRegistration = true,
            IsUsingPrivateCACertificate = false,
            Description = "智能风扇系列 型号VX100"
        };

        var exception2 = await Record.ExceptionAsync(async () =>
            await _productAppService.CreateAsync(input2));

        // Assert
        // 验证异常类型
        Assert.NotNull(exception2);
        var businessEx = Assert.IsType<BusinessException>(exception2);

        // 验证异常属性（如消息、错误码）
        Assert.Equal(IoTHubErrorCodes.ProductNameInvalid, businessEx.Code);
    }

    [Fact]
    public async Task CreateProduct_DuplicateNamenAsync()
    {
        var productName = "智能SmartFan_VX100_重名";

        // Arrange
        var input1 = new CreateProductInput
        {
            ProductName = productName,
            Category = ProductCategory.CustomCategory,
            CategoryName = "智能风扇VX系列",
            NodeType = ProductNodeTypes.DirectConnectionEquipment,
            NetworkingMode = ProductNetworkingModes.WiFi,
            AccessGatewayProtocol = null,
            DataFormat = ProductDataFormat.ICAStandardDataFormat,
            AuthenticationMode = ProductAuthenticationMode.DeviceSecret,
            IsEnableDynamicRegistration = true,
            IsUsingPrivateCACertificate = false,
            Description = "用于测试产品名称是否重名。"
        };

        var input2 = new CreateProductInput
        {
            ProductName = productName,
            Category = ProductCategory.CustomCategory,
            CategoryName = "智能风扇VX系列",
            NodeType = ProductNodeTypes.DirectConnectionEquipment,
            NetworkingMode = ProductNetworkingModes.WiFi,
            AccessGatewayProtocol = null,
            DataFormat = ProductDataFormat.ICAStandardDataFormat,
            AuthenticationMode = ProductAuthenticationMode.DeviceSecret,
            IsEnableDynamicRegistration = true,
            IsUsingPrivateCACertificate = false,
            Description = "用于测试产品名称是否重名。"
        };

        var product1 = await _productAppService.CreateAsync(input1);
        Assert.NotNull(product1);

        var exception = await Record.ExceptionAsync(async () =>
            await _productAppService.CreateAsync(input2));

        // Assert
        // 验证异常类型
        Assert.NotNull(exception);
        var businessEx = Assert.IsType<BusinessException>(exception);

        // 验证异常属性（如消息、错误码）
        Assert.Equal(IoTHubErrorCodes.DuplicateProductName, businessEx.Code);

        //// Assert
        //// 验证异常类型
        //Assert.NotNull(exception);
        //var userFriendlyEx = Assert.IsType<UserFriendlyException>(exception);

        //// 验证异常属性（如消息、错误码）
        //Assert.NotNull(userFriendlyEx.Message);
    }


}
