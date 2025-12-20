using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Artizan.IoTHub.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.Products;

public class EfCoreProductRepository : 
    EfCoreRepository<IIoTHubDbContext, Product, Guid>,
    IProductRepository
{
    public EfCoreProductRepository(IDbContextProvider<IIoTHubDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    public virtual async Task<Product?> FindByProductKeyAsync(string productKey, CancellationToken cancellationToken = default)
    {
        return await(await GetDbSetAsync())
            .SingleOrDefaultAsync(p => p.ProductKey == productKey, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<Product> GetByProductKeyAsync(string productKey, CancellationToken cancellationToken = default)
    {
        return await GetAsync(p => p.ProductKey == productKey, cancellationToken: GetCancellationToken(cancellationToken));
    }

    public async Task<Guid> GetIdByProductKeyAsync(string productKey, CancellationToken cancellationToken = default)
    {
        var id = await (await GetDbSetAsync())
            .Where(p => p.ProductKey == productKey)
            .Select(p => p.Id)
            .SingleAsync(cancellationToken: GetCancellationToken(cancellationToken));

        return id;
    }

    public async Task<string> GetProductKeyAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var id = await (await GetDbSetAsync())
            .Where(p => p.Id == productId)
            .Select(p => p.ProductKey)
            .SingleAsync(cancellationToken: GetCancellationToken(cancellationToken));
     
        return id;
    }

    public virtual async Task<Product?> FindByNameAsync(string productName, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .SingleOrDefaultAsync(p => p.ProductName == productName, GetCancellationToken(cancellationToken));
    }


}
