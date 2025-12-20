using Volo.Abp;
using Volo.Abp.MongoDB;

namespace Artizan.IoT.MongoDB;

public static class IoTMongoDbContextExtensions
{
    public static void ConfigureIoT(
        this IMongoModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
    }
}
