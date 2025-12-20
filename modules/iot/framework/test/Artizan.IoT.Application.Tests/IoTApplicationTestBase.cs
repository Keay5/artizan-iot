using Volo.Abp.Modularity;

namespace Artizan.IoT;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class IoTApplicationTestBase<TStartupModule> : IoTTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
