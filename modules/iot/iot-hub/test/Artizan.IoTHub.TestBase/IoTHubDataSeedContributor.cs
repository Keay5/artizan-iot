using Artizan.IoTHub.Products;
using Artizan.IoT.Products.Properties;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace Artizan.IoTHub;

public class IoTHubDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IProductRepository _productRepository;
    private readonly ProductManager _productManager;

    public IoTHubDataSeedContributor(
        IGuidGenerator guidGenerator, ICurrentTenant currentTenant,
        IProductRepository productRepository,
        ProductManager productManager)
    {
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
        _productRepository = productRepository;
        _productManager = productManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        /* Instead of returning the Task.CompletedTask, you can insert your test data
         * at this point!
         */

        using (_currentTenant.Change(context?.TenantId))
        {
        }
    }
}
