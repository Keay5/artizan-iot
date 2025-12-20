using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Products;

/* Write your custom repository tests like that, in this project, as abstract classes.
 * Then inherit these abstract classes from EF Core & MongoDB test projects.
 * In this way, both database providers are tests with the same set tests.
 */
public abstract class ProductRepository_Tests<TStartupModule> : IoTHubTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProductRepository _productRepository;

    protected ProductRepository_Tests()
    {
        _productRepository = GetRequiredService<IProductRepository>();
    }

    [Fact]
    public Task Method1Async()
    {
        return Task.CompletedTask;
    }
}