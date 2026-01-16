using InfluxDB.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoT.TimeSeries.InfluxDB;

/// <summary>
/// InfluxDB客户端工厂接口
/// 设计思路：封装客户端创建逻辑，保证单例复用
/// 设计模式：工厂模式（Factory Pattern）+ 单例模式（Singleton）
/// 设计考量：
/// 1. 客户端是重量级对象，需单例复用避免性能损耗
/// 2. 懒加载创建，避免应用启动时不必要的资源消耗
/// 3. 统一管理客户端生命周期，确保资源正确释放
/// </summary>
public interface IInfluxDbClientFactory : IDisposable
{
    /// <summary>
    /// 获取客户端实例（单例）
    /// </summary>
    /// <returns>InfluxDBClient实例</returns>
    InfluxDBClient GetClient();

    /// <summary>
    /// 检查客户端连接状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否正常</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
