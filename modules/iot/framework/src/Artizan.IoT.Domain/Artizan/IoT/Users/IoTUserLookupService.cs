using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace Artizan.IoT.Users;

public class IoTUserLookupService : UserLookupService<IoTUser, IIoTUserRepository>, IIoTUserLookupService
{
    public IoTUserLookupService(
        IIoTUserRepository userRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(
            userRepository,
            unitOfWorkManager)
    {

    }

    protected override IoTUser CreateUser(IUserData externalUser)
    {
        return new IoTUser(externalUser);
    }
}
