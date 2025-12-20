using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Products;

public class DefaultProductSecretGenerator : IProductSecretGenerator, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;

    public DefaultProductSecretGenerator(IGuidGenerator GuidGenerator)
    { 
        _guidGenerator = GuidGenerator;
    }

    public string Create()
    {
        return _guidGenerator.Create().ToString("N") + _guidGenerator.Create().ToString("N");
    }
}
