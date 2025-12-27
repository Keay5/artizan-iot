using System;
using System.Text;

namespace Artizan.IoT.Tracing;

/// <summary>
/// 全链路追踪ID生成器
/// 【设计思路】：生成全局唯一、可追溯的TraceId，便于分布式系统问题排查
/// 【设计考量】：
/// 1. 包含时间戳+机器标识+随机数，保证全局唯一性
/// 2. 固定长度（29位），便于日志解析和检索
/// 3. 异常容错：获取机器标识失败时用随机数替代
/// 【设计模式】：静态工具类（无设计模式，纯功能封装）
/// </summary>
public static class TraceIdGenerator
{
    /// <summary>
    /// 机器标识（默认取主机名哈希后6位，保证唯一性）
    /// </summary>
    private static readonly string _machineId;

    /// <summary>
    /// 线程安全的随机数生成器
    /// </summary>
    private static readonly Random _random = new Random();

    /// <summary>
    /// 静态构造函数（初始化机器标识）
    /// </summary>
    static TraceIdGenerator()
    {
        try
        {
            // 获取主机名并计算哈希，作为机器标识
            var machineName = Environment.MachineName;
            var hashBytes = Encoding.UTF8.GetBytes(machineName);
            var hash = BitConverter.ToInt32(hashBytes, 0);
            _machineId = Math.Abs(hash).ToString("X6"); // 转为6位16进制字符串
        }
        catch
        {
            // 异常时生成随机机器标识，保证可用性
            _machineId = _random.Next(0, 999999).ToString("D6");
        }
    }

    /// <summary>
    /// 生成全局唯一的TraceId
    /// 格式：时间戳(17位) + 机器标识(6位) + 随机数(6位)
    /// 示例：20250101120000123ABC123456
    /// </summary>
    /// <returns>唯一TraceId</returns>
    public static string Generate()
    {
        // 时间戳：yyyyMMddHHmmssfff（17位）
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        // 随机数：6位数字
        var random = _random.Next(0, 999999).ToString("D6");

        return $"{timestamp}{_machineId}{random}";
    }
}
