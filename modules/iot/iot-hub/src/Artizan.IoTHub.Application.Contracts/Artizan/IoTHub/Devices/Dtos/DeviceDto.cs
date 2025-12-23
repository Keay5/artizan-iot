using Artizan.IoT.Devices;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Devices.Dtos;

public class DeviceDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid ProductId { get; set; }

    public string DeviceName { get; set; }

    public string DeviceSecret { get; set; }

    public string? RemarkName { get; set; }


    public bool IsActive { get; set; }
    public bool IsEnable { get; set; }


    public DeviceStatus Status { get; set; }

    public string Description { get; set; }


    public string ConcurrencyStamp { get; set; }
}
