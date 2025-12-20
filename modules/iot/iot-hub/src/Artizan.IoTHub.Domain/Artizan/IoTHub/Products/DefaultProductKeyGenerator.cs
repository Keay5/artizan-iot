using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Products;

public class DefaultProductKeyGenerator : IProductKeyGenerator, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;

    public DefaultProductKeyGenerator(IGuidGenerator GuidGenerator)
    { 
        _guidGenerator = GuidGenerator;
    }

    public string Create()
    {
        return _guidGenerator.Create().ToString("N");
    }
}
