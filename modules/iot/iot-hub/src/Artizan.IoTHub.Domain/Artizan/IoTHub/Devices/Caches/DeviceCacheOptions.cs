using System;

namespace Artizan.IoTHub.Devices.Caches;

public class DeviceCacheOptions
{
    public TimeSpan CacheAbsoluteExpiration { get; set; }

    public DeviceCacheOptions()
    {
        CacheAbsoluteExpiration = TimeSpan.FromHours(1);
    }
}
