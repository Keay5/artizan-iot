using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Artizan.IoTHub.Devices;

public abstract class DeviceManager_Tests<TStartupModule> : IoTHubDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly DeviceManager _deviceManager;

    public DeviceManager_Tests()
    {
        _deviceManager = GetRequiredService<DeviceManager>();
    }

    [Fact]
    public Task Method1Async()
    {
        return Task.CompletedTask;
    }
}