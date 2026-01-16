using System;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Thing;

/// <summary>
/// 设备标识（ThingIdentifier）生成工具类
/// 统一管理设备唯一标识的生成规则，确保全系统遵循相同契约
/// </summary>
public class ThingIdentifierGenerator: IThingIdentifierGenerator, ISingletonDependency
{
    /// <summary>
    /// 分隔符：用于拼接ProductKey和DeviceName的固定字符
    /// 作为契约的一部分，统一维护避免多处硬编码
    /// </summary>
    protected const char ThingIdentifierSeparator = '&';

    /// <summary>
    /// 根据产品标识和设备名称生成唯一的 ThingIdentifier
    /// 遵循契约：{ProductKey}{分隔符}{DeviceName}
    /// </summary>
    /// <param name="productKey">产品Key（不能为空或空白）</param>
    /// <param name="deviceName">设备名称（不能为空或空白）</param>
    /// <returns>符合契约的 ThingIdentifier</returns>
    /// <exception cref="ArgumentException">当入参为空/空白时抛出</exception>
    public string Generate(string productKey, string deviceName)
    {
        // 严格的入参校验，保障标识的有效性
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentException("产品Key不能为空或空白", nameof(productKey));
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("设备名称不能为空或空白", nameof(deviceName));
        }

        // 使用常量分隔符，避免硬编码，便于后续统一修改
        return $"{productKey}{ThingIdentifierSeparator}{deviceName}";
    }

    /// <summary>
    /// 解析ThingIdentifier，还原出ProductKey和DeviceName
    /// 配套生成逻辑的反向操作，保障契约的完整性
    /// </summary>
    /// <param name="thingIdentifier">符合契约的设备标识</param>
    /// <returns>包含ProductKey和DeviceName的元组</returns>
    /// <exception cref="ArgumentException">当标识格式不符合契约时抛出</exception>
    public (string ProductKey, string DeviceName) Parse(string thingIdentifier)
    {
        if (string.IsNullOrWhiteSpace(thingIdentifier))
        {
            throw new ArgumentException("设备标识不能为空或空白", nameof(thingIdentifier));
        }

        var separatorIndex = thingIdentifier.IndexOf(ThingIdentifierSeparator);
        if (separatorIndex == -1 || separatorIndex == 0 || separatorIndex == thingIdentifier.Length - 1)
        {
            throw new ArgumentException($"设备标识格式不符合契约，正确格式：{{ProductKey}}{ThingIdentifierSeparator}{{DeviceName}}", nameof(thingIdentifier));
        }

        var productKey = thingIdentifier[..separatorIndex];
        var deviceName = thingIdentifier[(separatorIndex + 1)..];

        return (productKey, deviceName);
    }
}
