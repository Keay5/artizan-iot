using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Devices;

public class DefaultDeviceSecretGenerator : IDeviceSecretGenerator, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;

    public DefaultDeviceSecretGenerator(IGuidGenerator GuidGenerator)
    { 
        _guidGenerator = GuidGenerator;
    }

    public string Create()
    {
        return $"{_guidGenerator.Create().ToString("N")}{_guidGenerator.Create().ToString("N")}";
    }
}
