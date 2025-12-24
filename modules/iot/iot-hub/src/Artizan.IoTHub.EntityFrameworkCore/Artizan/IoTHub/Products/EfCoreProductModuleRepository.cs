using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Artizan.IoTHub.EntityFrameworkCore;
using Artizan.IoTHub.Products.Modules;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.Products;

public class EfCoreProductModuleRepository : 
    EfCoreRepository<IIoTHubDbContext, ProductModule, Guid>,
    IProductModuleRepository
{
    public EfCoreProductModuleRepository(IDbContextProvider<IIoTHubDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    public virtual async Task<ProductModule?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .SingleOrDefaultAsync(p => p.Name == name, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<List<ProductModule>> GetCurrentVersionListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await(await GetDbSetAsync())
            .Where(p => p.ProductId == productId && p.IsCurrentVersion)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}
