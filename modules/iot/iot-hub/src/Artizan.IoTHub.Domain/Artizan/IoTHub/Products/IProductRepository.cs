using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Artizan.IoTHub.Products;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<Product?> FindByProductKeyAsync(string productKey, CancellationToken cancellationToken = default);
    Task<Product> GetByProductKeyAsync(string productKey, CancellationToken cancellationToken = default);
    Task<Guid> GetIdByProductKeyAsync(string productKey, CancellationToken cancellationToken = default);
    Task<string> GetProductKeyAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Product?> FindByNameAsync(string productName, CancellationToken cancellationToken = default);
}
