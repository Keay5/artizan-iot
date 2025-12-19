using Volo.Abp.Modularity;

namespace Artizan.IoTDA;

/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class IoTDADomainTestBase<TStartupModule> : IoTDATestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
