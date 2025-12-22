using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class IoTHubDomainTestBase<TStartupModule> : IoTHubTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
