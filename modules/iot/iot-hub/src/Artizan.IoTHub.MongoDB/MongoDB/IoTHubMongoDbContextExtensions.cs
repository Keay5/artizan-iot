using Volo.Abp;
using Volo.Abp.MongoDB;

namespace Artizan.IoTHub.MongoDB;

public static class IoTHubMongoDbContextExtensions
{
    public static void ConfigureIoTHub(
        this IMongoModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
    }
}
