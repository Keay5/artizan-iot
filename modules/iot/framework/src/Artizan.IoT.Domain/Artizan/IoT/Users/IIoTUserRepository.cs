using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace Artizan.IoT.Users;

public interface IIoTUserRepository : IBasicRepository<IoTUser, Guid>, IUserRepository<IoTUser>
{
    Task<List<IoTUser>> GetUsersAsync(int maxCount, string filter, CancellationToken cancellationToken = default);
}