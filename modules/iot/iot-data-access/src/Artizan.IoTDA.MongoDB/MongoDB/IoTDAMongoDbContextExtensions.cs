using Volo.Abp;
using Volo.Abp.MongoDB;

namespace Artizan.IoTDA.MongoDB;

public static class IoTDAMongoDbContextExtensions
{
    public static void ConfigureIoTDA(
        this IMongoModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
    }
}
