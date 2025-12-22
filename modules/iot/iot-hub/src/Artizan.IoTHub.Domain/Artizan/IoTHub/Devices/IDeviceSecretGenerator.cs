using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoTHub.Devices
{
    public interface IDeviceSecretGenerator
    {
        string Create();
    }
}
