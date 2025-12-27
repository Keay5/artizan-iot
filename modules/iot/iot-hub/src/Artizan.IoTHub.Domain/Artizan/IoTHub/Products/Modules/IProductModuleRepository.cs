using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Volo.Abp.Domain.Repositories;

namespace Artizan.IoTHub.Products.Modules;

public interface IProductModuleRepository : IRepository<ProductModule, Guid>
{
    Task<ProductModule?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<ProductModule>> GetCurrentVersionListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
