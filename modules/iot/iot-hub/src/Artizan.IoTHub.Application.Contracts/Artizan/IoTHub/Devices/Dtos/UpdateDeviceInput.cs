using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Devices.Dtos;

public class UpdateDeviceInput : IHasConcurrencyStamp
{
    public string DeviceName { get; set; }
    public string? RemarkName { get; set; }
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; }
}
