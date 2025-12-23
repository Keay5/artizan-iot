using Artizan.IoT.Products.MessageParsings;
using Artizan.IoTHub.EntityFrameworkCore;
using Artizan.IoTHub.Products.MessageParsings;
using Artizan.IoTHub.Products.Modules;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Artizan.IoTHub.Products;

public class EfCoreProductMessageParserRepository : 
    EfCoreRepository<IIoTHubDbContext, ProductMessageParser, Guid>,
    IProductMessageParserRepository
{
    public EfCoreProductMessageParserRepository(IDbContextProvider<IIoTHubDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    public virtual async Task<ProductMessageParser?> FindPublishedByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await(await GetDbSetAsync())
            .SingleOrDefaultAsync(
                p => p.ProductId == productId && p.Status == ProductMessageParserStatus.Pulished, 
                GetCancellationToken(cancellationToken)
            );
    }

}
