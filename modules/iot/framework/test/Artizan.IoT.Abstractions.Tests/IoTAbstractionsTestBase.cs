using Volo.Abp.Modularity;

namespace Artizan.IoT.Abstractions.Tests;
/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class IoTAbstractionsTestBase<TStartupModule> : IoTTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}