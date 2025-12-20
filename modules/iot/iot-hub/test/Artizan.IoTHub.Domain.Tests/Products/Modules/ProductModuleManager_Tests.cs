using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products.Modules;

public abstract class ProductModuleManager_Tests<TStartupModule> : IoTHubDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ProductModuleManager _productModuleManager;

    public ProductModuleManager_Tests()
    {
        _productModuleManager = GetRequiredService<ProductModuleManager>();
    }

    [Fact]
    public Task Method1Async()
    {
        return Task.CompletedTask;
    }
}