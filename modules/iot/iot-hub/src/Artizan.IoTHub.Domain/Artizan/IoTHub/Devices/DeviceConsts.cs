using System.Text.RegularExpressions;

namespace Artizan.IoTHub.Devices;

public static class DeviceConsts
{
    /// <summary>
    /// 设备名称正则表达式（4-32字符，支持字母/数字/_/-/@/./:）
    /// </summary>
    public const string DeviceNameRegexPattern = Artizan.IoT.Devices.DeviceConsts.DeviceNameRegexPattern;

    /// <summary>
    /// 设备名称正则验证实例
    /// （RegexOptions.Compiled：预编译，重复使用高效）
    /// </summary>
    public static readonly Regex DeviceNameRegex = Artizan.IoT.Devices.DeviceConsts.DeviceNameRegex;

    public const int MaxDeviceNameLength = 32;
    public const int MinDeviceNameLength = 4;
    /// <summary>
    /// 设备名称长度为 4~32 个字符，可以包含英文字母、数字和特殊字符：短划线（-）、下划线（_）、at（@）、半角句号（.）、半角冒号（:）。
    /// </summary>
    public const string DeviceNameCharRegexPattern = @"^[-a-zA-Z0-9_@.:]{4,32}$";

    public const int MaxDeviceSecretLength = 128;
    public const int MinDeviceSecretLength = 8;

    public const int MinDeviceRemarkNameLength = 4;
    public const int MaxDeviceRemarkNameLength = 64;
    /// <summary>
    /// \u4e00-\u9fa5：匹配所有中文汉字（Unicode 编码范围，覆盖绝大多数常用简体 / 繁体汉字
    /// a-zA-Z：匹配所有英文字母（a-z小写，A-Z大写）；
    /// 0-9：匹配所有阿拉伯数字；
    /// _：匹配下划线。
    /// +表示匹配前面的字符集合至少 1 次（即整个字符串不能为空，且每个字符都属于上述字符集）
    /// </summary>
    public const string DeviceRemarkNameCharRegexPattern = @"^[\u4e00-\u9fa5a-zA-Z0-9_]+$";
    public const int MaxDescriptionLength = 128;
}
