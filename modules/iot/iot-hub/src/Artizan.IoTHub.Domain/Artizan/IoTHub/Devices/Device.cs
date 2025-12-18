using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Artizan.IoTHub.Devices;

public class Device : FullAuditedAggregateRoot<Guid>
{
    public virtual Guid ProductId { get; protected set; }
    /// <summary>
    ///  同一产品下，设备名必须唯一。 
    ///  DeviceName 通常与 ProductKey 组合使用，用作设备标识。
    /// </summary>
    [NotNull]
    public virtual string DeviceName { get; protected set; }

    [NotNull]
    public virtual string DeviceSecret { get; protected set; }

    [CanBeNull]
    public virtual string? RemarkName { get; protected set; }

    [NotNull]
    public virtual bool IsActive { get; set; }

    [NotNull]
    public virtual bool IsEnable { get; protected set; }

    [NotNull]
    public virtual DeviceStatus Status { get; protected set; }

    [CanBeNull]
    public virtual string? Description { get; protected set; }

    protected Device()
    {

    }

    public Device(
        Guid id,
        [NotNull] Guid productId,
        [NotNull] string deviceName,
        [NotNull] string deviceSecret,
        [CanBeNull] string? remarkName,
        [NotNull] bool isActive,
        [NotNull] bool isEnable,
        [NotNull] DeviceStatus status,
        [CanBeNull] string? description)
    {
        Id = id;
        ProductId = productId;
        SetDeviceName(deviceName);
        SetDeviceSecret(deviceSecret);
        SetRemarkName(remarkName);
        IsActive = isActive;
        SetEnable(isEnable);
        SetStatus(status);
        SetDescription(description);
    }

    public virtual Device SetDeviceName([NotNull] string deviceName)
    {
        Check.NotNullOrWhiteSpace(deviceName, nameof(deviceName));

        if (deviceName.Length < DeviceConsts.MinDeviceNameLength ||
            deviceName.Length > DeviceConsts.MaxDeviceNameLength ||
            !Regex.IsMatch(deviceName, DeviceConsts.DeviceNameCharRegexPattern)
            )
        {
            throw new BusinessException(IoTHubErrorCodes.DeviceNameInvalid)
                .WithData("MinLength", DeviceConsts.MinDeviceNameLength)
                .WithData("MaxLength", DeviceConsts.MaxDeviceNameLength);
        }

        DeviceName = deviceName;

        return this;
    }

    public virtual Device SetDeviceSecret([NotNull] string deviceSecret)
    {
        Check.NotNullOrWhiteSpace(deviceSecret, nameof(deviceSecret), DeviceConsts.MaxDeviceSecretLength, DeviceConsts.MinDeviceSecretLength);
        DeviceSecret = deviceSecret;

        return this;
    }

    public virtual Device SetRemarkName([CanBeNull] string? remarkName)
    {
        if (remarkName.IsNullOrEmpty())
        {
            return this;
        }

        if (remarkName.Length < DeviceConsts.MinDeviceRemarkNameLength ||
            remarkName.Length > DeviceConsts.MaxDeviceRemarkNameLength ||
            !Regex.IsMatch(remarkName, DeviceConsts.DeviceRemarkNameCharRegexPattern
           )
    )
        {
            throw new BusinessException(IoTHubErrorCodes.DeviceRemarkNameInvalid)
                .WithData("MinLength", DeviceConsts.MinDeviceRemarkNameLength)
                .WithData("MaxLength", DeviceConsts.MaxDeviceRemarkNameLength);
        }

        RemarkName = remarkName;

        return this;
    }

    public virtual Device SetEnable(bool isEnable)
    {
        IsEnable = isEnable;
        // TODO: 发起领域事件？
        return this;
    }
    public virtual Device SetStatus(DeviceStatus status)
    {
        Status = status;
        // TODO: 发起领域事件？
        return this;
    }

    public virtual Device SetDescription([CanBeNull] string? description)
    {
        if (description.IsNullOrEmpty())
        {
            return this;
        }

        if (!description.IsNullOrEmpty() && description.Length > DeviceConsts.MaxDescriptionLength)
        {
            throw new BusinessException(IoTHubErrorCodes.DescriptionInvalid)
                .WithData("MaxDescriptionLength", DeviceConsts.MaxDescriptionLength);
        }

        Description = description;

        return this;
    }

}
