using Volo.Abp.Modularity;

namespace Artizan.IoTHub;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class IoTHubApplicationTestBase<TStartupModule> : IoTHubTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
