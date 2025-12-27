using System;

namespace Artizan.IoT.Utils;

/// <summary>
/// 时间戳（Unix）工具类
/// 专注于Unix时间戳与DateTime的转换，默认适配UTC时区
/// 示例：
/// DateTime TimeStamp = TimeStampUtil.TryConvertMillisecondsTimestampToUtcDateTime(Time) ?? DateTime.UtcNow;
/// </summary>
public static class TimeStampUtil
{
    // 缓存Unix纪元基准时间（UTC），避免重复创建，提升性能
    private static readonly DateTime _unixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 【安全转换】尝试将UTC毫秒级Unix时间戳转换为UTC DateTime
    /// </summary>
    /// <param name="msTimestamp">UTC毫秒级Unix时间戳（可为null）</param>
    /// <returns>
    /// 转换成功：返回对应UTC DateTime；
    /// 转换失败（空值/超出DateTime范围）：返回null
    /// </returns>
    public static DateTime? TryConvertMillisecondsTimestampToUtcDateTime(long? msTimestamp)
    {
        if (msTimestamp is null) return null;

        try
        {
            return _unixEpochUtc.AddMilliseconds(msTimestamp.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null; // 时间戳超出DateTime范围时返回null，外层自行兜底
        }
    }
}
