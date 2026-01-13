using System.Text.RegularExpressions;

namespace Artizan.IoT.Devices;

public static class DeviceConsts
{
    /// <summary>
    /// 设备名称正则表达式（4-32字符，支持字母/数字/_/-/@/./:）
    /// </summary>
    public const string DeviceNameRegexPattern = @"^[a-zA-Z0-9_\-@.:]{4,32}$";

    /// <summary>
    /// 设备名称正则验证实例
    /// （RegexOptions.Compiled：预编译，重复使用高效）
    /// </summary>
    public static readonly Regex DeviceNameRegex = new Regex(DeviceNameRegexPattern, RegexOptions.Compiled);
}
