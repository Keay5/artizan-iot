using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Devices;

public static class DeviceConsts
{
    public static int MinDeviceNameLength { get; set; } = 4;
    public static int MaxDeviceNameLength { get; set; } = 32;
    public static int MaxDeviceSecretLength { get; set; } = 128;
    public static int MinRemarkNameLength { get; set; } = 4;
    public static int MaxRemarkNameLength { get; set; } = 64;
    public static int MaxDescriptionLength { get; set; } = 256;
}
