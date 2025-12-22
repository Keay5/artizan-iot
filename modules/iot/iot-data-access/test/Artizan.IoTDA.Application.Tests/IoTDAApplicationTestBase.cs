using Volo.Abp.Modularity;

namespace Artizan.IoTDA;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class IoTDAApplicationTestBase<TStartupModule> : IoTDATestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
