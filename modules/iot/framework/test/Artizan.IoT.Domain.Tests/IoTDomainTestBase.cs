using Volo.Abp.Modularity;

namespace Artizan.IoT;

/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class IoTDomainTestBase<TStartupModule> : IoTTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
