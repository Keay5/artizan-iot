using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products;

public abstract class ProductManager_Tests<TStartupModule> : IoTHubDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ProductManager _productManager;

    public ProductManager_Tests()
    {
        _productManager = GetRequiredService<ProductManager>();
    }

    [Fact]
    public Task Method1Async()
    {
        return Task.CompletedTask;
    }
}