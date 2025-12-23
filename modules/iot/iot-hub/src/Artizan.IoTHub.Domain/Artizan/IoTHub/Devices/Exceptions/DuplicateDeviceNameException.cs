using Volo.Abp;

namespace Artizan.IoTHub.Devices.Exceptions;

public class DuplicateDeviceNameException : BusinessException
{
    public DuplicateDeviceNameException(string deviceName)
        : base(IoTHubErrorCodes.DuplicateDeviceName)
    {
        WithData("DeviceName", deviceName);
    }
}
