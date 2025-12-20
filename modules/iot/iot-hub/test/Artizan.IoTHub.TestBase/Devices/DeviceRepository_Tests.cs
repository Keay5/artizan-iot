using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Devices;

/* Write your custom repository tests like that, in this project, as abstract classes.
 * Then inherit these abstract classes from EF Core & MongoDB test projects.
 * In this way, both database providers are tests with the same set tests.
 */
public abstract class DeviceRepository_Tests<TStartupModule> : IoTHubTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IDeviceRepository _deviceRepository;

    protected DeviceRepository_Tests()
    {
        _deviceRepository = GetRequiredService<IDeviceRepository>();
    }

    [Fact]
    public Task Method1Async()
    {
        return Task.CompletedTask;
    }
}
