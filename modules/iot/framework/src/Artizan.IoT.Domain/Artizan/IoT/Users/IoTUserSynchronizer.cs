using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace Artizan.IoT.Users;

public class IoTUserSynchronizer :
    IDistributedEventHandler<EntityUpdatedEto<UserEto>>,
    ITransientDependency
{
    protected IIoTUserRepository UserRepository { get; }
    protected IIoTUserLookupService UserLookupService { get; }

    public IoTUserSynchronizer(
        IIoTUserRepository userRepository,
        IIoTUserLookupService userLookupService)
    {
        UserRepository = userRepository;
        UserLookupService = userLookupService;
    }

    public async Task HandleEventAsync(EntityUpdatedEto<UserEto> eventData)
    {
        var user = await UserRepository.FindAsync(eventData.Entity.Id);
        if (user == null)
        {
            user = await UserLookupService.FindByIdAsync(eventData.Entity.Id);
            if (user == null)
            {
                return;
            }
        }

        if (user.Update(eventData.Entity))
        {
            await UserRepository.UpdateAsync(user);
        }
    }
}
