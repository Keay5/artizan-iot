using System;

namespace Artizan.IoTHub.Devices.Dtos;

public class CreateDeviceInput
{
    public Guid ProductId { get; set; }

    public string DeviceName { get; set; }

    public string? RemarkName { get; set; }
}
