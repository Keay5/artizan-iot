using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Artizan.IoTHub.Products.MessageParsings;

public interface IProductMessageParserRepository : IRepository<ProductMessageParser, Guid>
{
    Task<ProductMessageParser?> FindPublishedByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
